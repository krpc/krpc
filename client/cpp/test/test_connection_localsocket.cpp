#include <asio/local/stream_protocol.hpp>
#include <cstdlib>
#include <filesystem>
#include <memory>
#include <random>
#include <sstream>
#include <string>
#include <system_error>

#include "connection_test.hpp"
#include "gtest/gtest.h"
#include "krpc/connection.hpp"

namespace {

using local_stream = asio::local::stream_protocol;

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

class localsocket_transport {
 public:
  void start() {
    // A socket path has to fit in the kernel's address structure, which leaves far less
    // room than a path named after the test would take, so it goes in a short temporary
    // directory of its own
    directory = make_directory();
    path = (directory / "rpc").string();
    server.reset(new echo_server<local_stream>(local_stream::endpoint(path)));
  }

  void stop() {
    server.reset();
    std::error_code error;
    std::filesystem::remove_all(directory, error);
  }

  std::shared_ptr<krpc::Connection> connect() {
    auto connection = std::make_shared<krpc::LocalConnection>(path);
    connection->connect();
    return connection;
  }

  std::filesystem::path directory;
  std::string path;
  std::unique_ptr<echo_server<local_stream>> server;

 private:
  /** A directory no other test is using, whether it belongs to this process or another.
      The name is drawn at random and create_directory reports one already taken rather than
      taking it over, so the first name it accepts is ours alone. Counting up from a fixed
      name instead would make every run walk over the directories that runs killed before
      they could clean up had left behind. */
  static std::filesystem::path make_directory() {
    std::random_device source;
    for (;;) {
      std::ostringstream name;
      name << "krpc-cpp-test-" << std::hex << source() << source();
      auto candidate = socket_directory() / name.str();
      if (std::filesystem::create_directory(candidate)) return candidate;
    }
  }
};

}  // namespace

INSTANTIATE_TYPED_TEST_SUITE_P(localsocket, connection_test, localsocket_transport);

TEST(test_connection_localsocket, connect_to_a_path_nothing_is_listening_on) {
  auto missing = socket_directory() / "krpc-cpp-test-does-not-exist";
  krpc::LocalConnection connection(missing.string());
  ASSERT_THROW(connection.connect(), std::exception);
}
