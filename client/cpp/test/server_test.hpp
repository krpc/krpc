#ifndef HEADER_KRPC_TEST_SERVER_TEST
#define HEADER_KRPC_TEST_SERVER_TEST

#include <krpc.hpp>
#include "services/test_service.hpp"

class server_test: public ::testing::Test {
public:
  server_test();
  krpc::Client connect();
  krpc::Client conn;
  krpc::services::KRPC krpc;
  krpc::services::TestService test_service;
};

inline server_test::server_test():
  conn(connect()),
  krpc(&conn),
  test_service(&conn) {}

inline krpc::Client server_test::connect() {
  char* env_rpc_port = std::getenv("RPC_PORT");
  char* env_stream_port = std::getenv("STREAM_PORT");
  int rpc_port = env_rpc_port == nullptr ? 50000 : std::stoi(env_rpc_port);
  int stream_port = env_stream_port == nullptr ? 50001 : std::stoi(env_stream_port);
  return krpc::connect("C++ClientTest", "localhost", rpc_port, stream_port);
}

#endif
