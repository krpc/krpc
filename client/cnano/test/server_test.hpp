#pragma once

#include <gmock/gmock.h>
#include <krpc_cnano.h>

#include <cstdint>
#include <cstdlib>

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
  krpc_connection_t conn;
};

inline server_test::server_test() : conn(connect()) {}

inline server_test::~server_test() {
  if (KRPC_OK != krpc_close(conn)) exit(1);
}

// The address the server the test harness started is listening on, in whatever form the
// transport this build uses opens a connection from.
#ifdef KRPC_COMMUNICATION_TCP
inline krpc_connection_config_t server_address() {
  // The port the test harness passes in, or the server's default when the binary is run directly
  const char* port = std::getenv("RPC_PORT");
  krpc_connection_config_t config;
  config.address = "127.0.0.1";
  config.port = port == nullptr ? 50000 : static_cast<uint16_t>(std::atoi(port));
  return config;
}
#elif defined(KRPC_COMMUNICATION_LOCALSOCKET)
inline const char* server_address() { return std::getenv("RPC_PATH"); }
#else
inline const char* server_address() { return std::getenv("PORT"); }
#endif

inline krpc_connection_t server_test::connect() {
  krpc_connection_t result;
#ifdef KRPC_COMMUNICATION_TCP
  krpc_connection_config_t config = server_address();
  if (KRPC_OK != krpc_open(&result, &config)) exit(1);
#else
  if (KRPC_OK != krpc_open(&result, server_address())) exit(1);
#endif
  if (KRPC_OK != krpc_connect(result, "TestClientCNano")) exit(1);
  return result;
}
