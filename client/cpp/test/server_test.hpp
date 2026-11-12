#pragma once

#include <chrono>
#include <functional>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/transport.hpp>
#include <memory>
#include <string>
#include <thread>

#include "services/test_service.hpp"

class server_test : public ::testing::Test {
 public:
  server_test();
  /** Connect to the server the harness started, on the port it passes. */
  krpc::Connection connect(const std::string& name = "C++ClientTest");
  /** A client of the test's own, which the test can destroy. */
  std::unique_ptr<krpc::Connection> connect_client();
  /** Whether condition becomes true within ten seconds. */
  static bool eventually(const std::function<bool()>& condition);
  int get_port() const;
  krpc::Connection conn;
  krpc::services::KRPC krpc;
  krpc::services::TestService test_service;
};

inline server_test::server_test() : conn(connect()), krpc(conn), test_service(conn) {}

inline krpc::Connection server_test::connect(const std::string& name) {
  return krpc::connect(name, "localhost", get_port());
}

inline std::unique_ptr<krpc::Connection> server_test::connect_client() {
  auto transport = std::make_shared<krpc::TCPIPTransport>("localhost", get_port());
  return std::make_unique<krpc::Connection>("C++ClientTest", transport);
}

inline bool server_test::eventually(const std::function<bool()>& condition) {
  auto deadline = std::chrono::steady_clock::now() + std::chrono::seconds(10);
  while (!condition()) {
    if (std::chrono::steady_clock::now() >= deadline) return false;
    std::this_thread::sleep_for(std::chrono::milliseconds(10));
  }
  return true;
}

inline int server_test::get_port() const {
  char* env_port = std::getenv("KRPC_TEST_TCPIP_PORT");
  return env_port == nullptr ? 50000 : std::stoi(env_port);
}
