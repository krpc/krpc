#pragma once

#include <asio/io_context.hpp>
#include <asio/ip/address.hpp>
#include <asio/ip/tcp.hpp>
#include <chrono>  // NOLINT(build/c++11)
#include <functional>
#include <krpc.hpp>
#include <krpc/connection.hpp>
#include <krpc/services/krpc.hpp>
#include <memory>
#include <string>
#include <thread>  // NOLINT(build/c++11)

#include "services/test_service.hpp"

class server_test : public ::testing::Test {
 public:
  server_test();
  /** Connect over whichever transport the harness started the server with, which it tells
      us about by port or by socket path. */
  krpc::Client connect(const std::string& name = "C++ClientTest");
  /** A client of the test's own, which the test closes by destroying it. Sets shutdown, if
      given, to a function that shuts down the client's connection, which stands in for the
      server closing it. */
  std::unique_ptr<krpc::Client> connect_client(std::function<void()>* shutdown = nullptr);
  /** Whether condition becomes true within ten seconds. */
  static bool eventually(const std::function<bool()>& condition);
  int get_port() const;
  const char* get_path() const;
  /** A port nothing is listening on, for the tests that connect to the wrong one. Binding a
      port and giving it straight back leaves one that a connection is refused on, and leaves
      it in the range the system hands out. A port derived from the server's own can land
      anywhere, including on a low one, and a connection to those is dropped rather than
      refused on Windows, which leaves the client waiting. */
  static int unused_port();
  /** How long a connection to a port nothing is listening on is waited for. It is normally
      refused at once; where the system drops the attempt instead, this bounds the wait
      rather than leaving the test to hang. */
  static constexpr std::chrono::milliseconds connect_timeout{std::chrono::seconds(10)};
  krpc::Client conn;
  krpc::services::KRPC krpc;
  krpc::services::TestService test_service;
};

inline server_test::server_test() : conn(connect()), krpc(&conn), test_service(&conn) {}

inline krpc::Client server_test::connect(const std::string& name) {
  if (get_path() != nullptr) return krpc::connect_local(name, get_path());
  return krpc::connect(name, "localhost", get_port());
}

inline std::unique_ptr<krpc::Client> server_test::connect_client(std::function<void()>* shutdown) {
  std::shared_ptr<krpc::Connection> connection;
  if (get_path() != nullptr)
    connection = std::make_shared<krpc::LocalConnection>(get_path());
  else
    connection = std::make_shared<krpc::Connection>("localhost", get_port());
  if (shutdown != nullptr) *shutdown = [connection] { connection->shutdown_both(); };
  return std::make_unique<krpc::Client>("C++ClientTest", connection);
}

inline bool server_test::eventually(const std::function<bool()>& condition) {
  auto deadline = std::chrono::steady_clock::now() + std::chrono::seconds(10);
  while (!condition()) {
    if (std::chrono::steady_clock::now() >= deadline) return false;
    std::this_thread::sleep_for(std::chrono::milliseconds(10));
  }
  return true;
}

inline const char* server_test::get_path() const {
  return std::getenv("KRPC_TEST_LOCALSOCKET_PATH");
}

inline int server_test::get_port() const {
  char* env_port = std::getenv("KRPC_TEST_TCPIP_PORT");
  return env_port == nullptr ? 50000 : std::stoi(env_port);
}

inline int server_test::unused_port() {
  asio::io_context io_context;
  asio::ip::tcp::acceptor acceptor(io_context);
  acceptor.open(asio::ip::tcp::v4());
  acceptor.bind(asio::ip::tcp::endpoint(asio::ip::make_address("127.0.0.1"), 0));
  return acceptor.local_endpoint().port();
}
