#ifndef HEADER_KRPC_TEST_SERVER_TEST
#define HEADER_KRPC_TEST_SERVER_TEST

#include <krpc/krpc.hpp>

class server_test: public ::testing::Test {
public:
  server_test();
  krpc::Client conn;
};

server_test::server_test():
  conn(krpc::connect("TestClient", "localhost", 50000, 50001)) {}

#endif
