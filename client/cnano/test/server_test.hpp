#pragma once

#include <gmock/gmock.h>
#include <krpc_cnano.h>

// Check the message for the last error returned by the server, when the client is built with
// support for them. Only the start of the message is compared, as the server also sends a stack
// trace, which is truncated to fit the message buffer.
#ifdef KRPC_ERROR_MESSAGES
#define ASSERT_ERROR_MESSAGE(expected) \
  ASSERT_THAT(krpc_get_error_message(), testing::StartsWith(expected))
#else
#define ASSERT_ERROR_MESSAGE(expected) \
  do {                                 \
  } while (false)
#endif

class server_test : public ::testing::Test {
 public:
  server_test();
  ~server_test();
  krpc_connection_t connect();
  const char* get_port() const;
  krpc_connection_t conn;
};

inline server_test::server_test() : conn(connect()) {}

inline server_test::~server_test() {
  if (KRPC_OK != krpc_close(conn)) exit(1);
}

inline krpc_connection_t server_test::connect() {
  krpc_connection_t result;
  if (KRPC_OK != krpc_open(&result, get_port())) exit(1);
  if (KRPC_OK != krpc_connect(result, "TestClientCNano")) exit(1);
  return result;
}

inline const char* server_test::get_port() const { return std::getenv("PORT"); }
