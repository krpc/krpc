#include <krpc_cnano/communication.h>
#include <string.h>

/* The sockets API, spelled for the platform. Winsock keeps the names and the layout of the
   address, so what the test does with them below is the same on both. The unix domain address
   is declared in a header of its own, which builds on what winsock2.h declares and so has to
   follow it; sorting these would put it first. */
/* clang-format off */
#ifdef _WIN32
#include <winsock2.h>
#include <afunix.h>
/* clang-format on */
#else
#include <sys/socket.h>
#include <sys/un.h>
#include <unistd.h>

typedef int SOCKET;
#define INVALID_SOCKET (-1)
#define closesocket close
#endif

#include <cstdlib>
#include <filesystem>
#include <string>
#include <system_error>

#include "gtest/gtest.h"

namespace {

/** A directory to put a socket in, short enough for the path of one to fit in a socket address.
    The directory a test is given for its temporary files is nested far deeper than an address has
    room for, so the platform's own is used directly. */
std::filesystem::path socket_directory() {
#ifdef _WIN32
  const char* local = std::getenv("LOCALAPPDATA");
  if (local != nullptr && local[0] != '\0') return std::filesystem::path(local) / "Temp";
  return std::filesystem::temp_directory_path();
#else
  return "/tmp";
#endif
}

/** Winsock started for as long as it is held, which it has to be before the first socket call
    in a process. The client starts it for the sockets it opens; the server below opens its own
    before any of that, so it starts winsock for itself rather than relying on the client having
    been asked to open a socket first. Starts are counted against stops, so holding one here
    costs nothing beyond the first. */
class Sockets {
 public:
  Sockets() {
#ifdef _WIN32
    WSADATA winsock;
    EXPECT_EQ(0, WSAStartup(MAKEWORD(2, 2), &winsock));
#endif
  }

  ~Sockets() {
#ifdef _WIN32
    WSACleanup();
#endif
  }

  Sockets(const Sockets&) = delete;
  Sockets& operator=(const Sockets&) = delete;
};

// A server that accepts one connection and echoes back everything sent to it, so that
// the client side of a connection can be exercised without a kRPC server.
class EchoServer {
 public:
  EchoServer() : listener_(INVALID_SOCKET), accepted_(INVALID_SOCKET) {
    // A socket path has to fit in the address structure, which leaves far less room than a
    // path named after the test would take, so it goes in a short temporary directory
    directory_ = socket_directory() / ("krpc-cnano-test-" + std::to_string(counter_++));
    std::filesystem::create_directories(directory_);
    path_ = (directory_ / "rpc").string();
    struct sockaddr_un address;
    memset(&address, 0, sizeof(address));
    address.sun_family = AF_UNIX;
    strncpy(address.sun_path, path_.c_str(), sizeof(address.sun_path) - 1);
    listener_ = socket(AF_UNIX, SOCK_STREAM, 0);
    // Checked here so that a server that never came up says so, rather than leaving the
    // client to report only that it could not connect to it
    EXPECT_NE(INVALID_SOCKET, listener_);
    EXPECT_EQ(0, bind(listener_, reinterpret_cast<struct sockaddr*>(&address),
                      static_cast<int>(sizeof(address))));
    EXPECT_EQ(0, listen(listener_, 1));
  }

  ~EchoServer() {
    if (accepted_ != INVALID_SOCKET) closesocket(accepted_);
    if (listener_ != INVALID_SOCKET) closesocket(listener_);
    std::error_code error;
    std::filesystem::remove_all(directory_, error);
  }

  const char* path() const { return path_.c_str(); }

  // Accept the pending connection. The client connects first, so this never blocks
  // for long.
  void accept_one() { accepted_ = accept(listener_, NULL, NULL); }

  void echo(size_t count) {
    char buffer[64];
    size_t total = 0;
    while (total < count) {
      int read_bytes = recv(accepted_, buffer, static_cast<int>(count - total), 0);
      if (read_bytes <= 0) return;
      send(accepted_, buffer, read_bytes, 0);
      total += static_cast<size_t>(read_bytes);
    }
  }

 private:
  // Declared first, so that winsock is started before the sockets below are opened and
  // stopped after they have been closed
  Sockets sockets_;
  std::filesystem::path directory_;
  std::string path_;
  SOCKET listener_;
  SOCKET accepted_;
  static int counter_;
};

int EchoServer::counter_ = 0;

// A path no socket was ever created at, in a directory that exists on every platform.
std::string missing_path() {
  return (socket_directory() / "krpc-cnano-test-does-not-exist").string();
}

}  // namespace

TEST(test_communication_localsocket, test_open_close) {
  EchoServer server;
  krpc_connection_t connection;
  ASSERT_EQ(KRPC_OK, krpc_open(&connection, server.path()));
  server.accept_one();
  ASSERT_EQ(KRPC_OK, krpc_close(connection));
}

TEST(test_communication_localsocket, test_write_and_read) {
  EchoServer server;
  krpc_connection_t connection;
  ASSERT_EQ(KRPC_OK, krpc_open(&connection, server.path()));
  server.accept_one();

  const uint8_t data[] = {1, 2, 3, 4, 5, 6, 7, 8};
  ASSERT_EQ(KRPC_OK, krpc_write(connection, data, sizeof(data)));
  server.echo(sizeof(data));

  uint8_t buffer[8] = {0};
  ASSERT_EQ(KRPC_OK, krpc_read(connection, buffer, sizeof(buffer)));
  for (size_t i = 0; i < sizeof(buffer); i++) ASSERT_EQ(data[i], buffer[i]);
  ASSERT_EQ(KRPC_OK, krpc_close(connection));
}

// A read that returns fewer bytes than asked for must resume where it left off
TEST(test_communication_localsocket, test_read_partial) {
  EchoServer server;
  krpc_connection_t connection;
  ASSERT_EQ(KRPC_OK, krpc_open(&connection, server.path()));
  server.accept_one();

  const uint8_t data[] = {1, 2, 3, 4, 5, 6, 7, 8};
  ASSERT_EQ(KRPC_OK, krpc_write(connection, data, 3));
  server.echo(3);
  ASSERT_EQ(KRPC_OK, krpc_write(connection, data + 3, 5));
  server.echo(5);

  uint8_t buffer[8] = {0};
  ASSERT_EQ(KRPC_OK, krpc_read(connection, buffer, sizeof(buffer)));
  for (size_t i = 0; i < sizeof(buffer); i++) ASSERT_EQ(data[i], buffer[i]);
  ASSERT_EQ(KRPC_OK, krpc_close(connection));
}

TEST(test_communication_localsocket, test_open_nonexistent) {
  krpc_connection_t connection;
  ASSERT_EQ(KRPC_ERROR_IO, krpc_open(&connection, missing_path().c_str()));
}

// The path is copied into a fixed size field, so one that does not fit is reported
// rather than silently truncated to name a different socket
TEST(test_communication_localsocket, test_open_path_too_long) {
  krpc_connection_t connection;
  std::string path(512, 'x');
  ASSERT_EQ(KRPC_ERROR_IO, krpc_open(&connection, path.c_str()));
}
