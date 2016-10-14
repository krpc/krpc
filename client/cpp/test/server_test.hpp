#pragma once

#include <krpc.hpp>
#include "services/test_service.hpp"

class server_test: public ::testing::Test {
 public:
  server_test();
  static int get_rpc_port();
  static int get_stream_port();
  krpc::Client connect();
  krpc::Client conn;
  krpc::services::KRPC krpc;
  krpc::services::TestService test_service;
};

inline server_test::server_test():
  conn(connect()),
  krpc(&conn),
  test_service(&conn) {}

inline int server_test::get_rpc_port() {
  char* env_rpc_port = std::getenv("RPC_PORT");
  return env_rpc_port == nullptr ? 50000 : std::stoi(env_rpc_port);
}

inline int server_test::get_stream_port() {
  char* env_stream_port = std::getenv("STREAM_PORT");
  return env_stream_port == nullptr ? 50001 : std::stoi(env_stream_port);
}

inline krpc::Client server_test::connect() {
  return krpc::connect("C++ClientTest", "localhost", get_rpc_port(), get_stream_port());
}
