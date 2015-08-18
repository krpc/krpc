#ifndef HEADER_KRPC_TEST_SERVER_TEST
#define HEADER_KRPC_TEST_SERVER_TEST

#include <krpc/krpc.hpp>

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
  return krpc::connect("TestClient", "localhost", 50011, 50012);
}

#endif
