#pragma once

#include <krpc_cnano.h>

class server_test: public ::testing::Test {
 public:
  server_test();
  ~server_test();
  krpc_connection_t connect();
  const char * get_port() const;
  krpc_connection_t conn;
};

inline server_test::server_test():
  conn(connect()) {}

inline server_test::~server_test() {
  if (KRPC_OK != krpc_close(conn))
    exit(1);
  conn = -1;
}

inline krpc_connection_t server_test::connect() {
  krpc_connection_t result;
  if (KRPC_OK != krpc_open(&result, get_port()))
    exit(1);
  if (KRPC_OK != krpc_connect(result, "TestClientCNano"))
    exit(1);
  return result;
}

inline const char * server_test::get_port() const {
  return std::getenv("PORT");
}
