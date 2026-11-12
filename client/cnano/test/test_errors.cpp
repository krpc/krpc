// The errors the client returns, at the detail level the library is built with: codes only,
// the details of the last error, or the details and the message

#include <krpc_cnano.h>
#include <krpc_cnano/encoder.h>
#include <krpc_cnano/pb_encode.h>

#include <cstring>
#include <deque>
#include <string>
#include <vector>

#include "gtest/gtest.h"

namespace {

// An in-memory link, with the messages from the server queued up front
struct memory_link {
  std::deque<uint8_t> incoming;
  std::string write_failure;
};

krpc_error_t memory_read(uintptr_t handle, uint8_t* buf, size_t count) {
  auto* link = reinterpret_cast<memory_link*>(handle);
  for (size_t i = 0; i < count; i++) {
    if (link->incoming.empty()) return KRPC_ERROR_EOF;
    buf[i] = link->incoming.front();
    link->incoming.pop_front();
  }
  return KRPC_OK;
}

krpc_error_t memory_write(uintptr_t handle, const uint8_t* buf, size_t count) {
  auto* link = reinterpret_cast<memory_link*>(handle);
  (void)buf;
  (void)count;
  if (link->write_failure.empty()) return KRPC_OK;
  krpc_set_error_cause(link->write_failure.c_str());
  return KRPC_ERROR_IO;
}

const krpc_transport_t memory_transport = {&memory_read, &memory_write, nullptr, false};

bool encode_error(pb_ostream_t* stream, const pb_field_t* field, void* const* arg) {
  return pb_encode_tag_for_field(stream, field) &&
         pb_encode_submessage(stream, krpc_schema_Error_fields, *arg);
}

class ErrorsTest : public ::testing::Test {
 protected:
  void SetUp() override {
    connection.transport = &memory_transport;
    connection.handle = reinterpret_cast<uintptr_t>(&link);
  }

  // Queue a message from the server, prefixed with its size. Message_init_default initializes
  // only the first case of the content union, so each message sets its own case to its default
  void queue(const krpc_schema_Message& message) {
    std::vector<uint8_t> data(1024);
    pb_ostream_t stream = pb_ostream_from_buffer(data.data(), data.size());
    ASSERT_TRUE(pb_encode_delimited(&stream, krpc_schema_Message_fields, &message));
    link.incoming.insert(link.incoming.end(), data.begin(), data.begin() + stream.bytes_written);
  }

  void queue_connection_response(krpc_schema_ConnectionResponse_Status status,
                                 const char* message) {
    krpc_schema_Message response = krpc_schema_Message_init_default;
    response.which_content = krpc_schema_Message_connection_response_tag;
    response.content.connection_response = krpc_schema_ConnectionResponse_init_default;
    response.content.connection_response.status = status;
    response.content.connection_response.protocol_version = 2;
    response.content.connection_response.message.funcs.encode = &krpc_encode_callback_cstring;
    response.content.connection_response.message.arg = const_cast<char*>(message);
    queue(response);
  }

  // Queue the response to the first call, the error Service.Name with ids 11 and 22
  void queue_server_error() {
    krpc_schema_Error error = krpc_schema_Error_init_default;
    error.service.funcs.encode = &krpc_encode_callback_cstring;
    error.service.arg = const_cast<char*>("Service");
    error.exception.funcs.encode = &krpc_encode_callback_cstring;
    error.exception.arg = const_cast<char*>("Name");
    error.description.funcs.encode = &krpc_encode_callback_cstring;
    error.description.arg = const_cast<char*>("Oops");
    error.service_id = 11;
    error.exception_id = 22;
    krpc_schema_Message message = krpc_schema_Message_init_default;
    message.which_content = krpc_schema_Message_response_tag;
    message.content.response = krpc_schema_Response_init_default;
    message.content.response.id = 1;
    message.content.response.error.funcs.encode = &encode_error;
    message.content.response.error.arg = &error;
    queue(message);
  }

  void connect() {
    queue_connection_response(krpc_schema_ConnectionResponse_Status_OK, "");
    ASSERT_EQ(KRPC_OK, krpc_connect(connection, "test"));
  }

  krpc_error_t invoke() {
    krpc_schema_ProcedureCall call = krpc_schema_ProcedureCall_init_default;
    krpc_schema_ProcedureResult result = krpc_schema_ProcedureResult_init_default;
    call.service_id = 1;
    call.procedure_id = 1;
    return krpc_invoke(connection, &result, &call);
  }

  memory_link link;
  krpc_connection_t connection;
};

}  // namespace

TEST_F(ErrorsTest, ServerError) {
  connect();
  queue_server_error();

  ASSERT_EQ(KRPC_ERROR_SERVER, invoke());
#ifndef KRPC_ERROR_CODES_ONLY
  const krpc_error_info_t* info = krpc_get_error_info();
  EXPECT_EQ(KRPC_ERROR_SERVER, info->code);
  EXPECT_EQ(-1, info->cause);
  EXPECT_EQ(11u, info->service);
  EXPECT_EQ(22u, info->exception);
  EXPECT_EQ(-1, info->status);
  EXPECT_EQ(-1, info->reason);
  EXPECT_TRUE(krpc_is_exception(11, 22));
  EXPECT_FALSE(krpc_is_exception(11, 23));
#endif
#ifdef KRPC_ERROR_MESSAGES
  EXPECT_STREQ("Service.Name: Oops", krpc_get_error_message());
#endif

  // The session goes on after a server error
  krpc_schema_Message ok = krpc_schema_Message_init_default;
  ok.which_content = krpc_schema_Message_response_tag;
  ok.content.response = krpc_schema_Response_init_default;
  ok.content.response.id = 2;
  ok.content.response.results_count = 1;
  queue(ok);
  ASSERT_EQ(KRPC_OK, invoke());
}

// A success leaves the last error as it was, including a call whose message is too long for
// the buffer, which is encoded again a bufferful at a time, and a disconnect the server misses
TEST_F(ErrorsTest, SuccessKeepsTheLastError) {
  connect();
  queue_server_error();
  ASSERT_EQ(KRPC_ERROR_SERVER, invoke());

  std::string long_string(size_t{2} * KRPC_BUFFER_SIZE, 'x');
  const char* value = long_string.c_str();
  krpc_argument_t arguments[1];
  krpc_call_t call;
  ASSERT_EQ(KRPC_OK, krpc_call(&call, 1, 1, 1, arguments));
  ASSERT_EQ(KRPC_OK, krpc_add_argument(&call, 0, &krpc_encode_callback_string,
                                       static_cast<const void*>(&value)));
  krpc_schema_Message ok = krpc_schema_Message_init_default;
  ok.which_content = krpc_schema_Message_response_tag;
  ok.content.response = krpc_schema_Response_init_default;
  ok.content.response.id = 2;
  ok.content.response.results_count = 1;
  queue(ok);
  krpc_schema_ProcedureResult result = krpc_schema_ProcedureResult_init_default;
  ASSERT_EQ(KRPC_OK, krpc_invoke(connection, &result, &call.message));

  link.write_failure = "Cable cut";
  ASSERT_EQ(KRPC_OK, krpc_disconnect(connection));
#ifndef KRPC_ERROR_CODES_ONLY
  EXPECT_EQ(KRPC_ERROR_SERVER, krpc_get_error_info()->code);
  EXPECT_EQ(11u, krpc_get_error_info()->service);
#endif
#ifdef KRPC_ERROR_MESSAGES
  EXPECT_STREQ("Service.Name: Oops", krpc_get_error_message());
#endif
}

TEST_F(ErrorsTest, NullBytes) {
  connect();
  krpc_bytes_t bytes = {5, nullptr};
  krpc_argument_t arguments[1];
  krpc_call_t call;
  ASSERT_EQ(KRPC_OK, krpc_call(&call, 1, 1, 1, arguments));
  ASSERT_EQ(KRPC_OK, krpc_add_argument(&call, 0, &krpc_encode_callback_bytes, &bytes));
  krpc_schema_ProcedureResult result = krpc_schema_ProcedureResult_init_default;
  ASSERT_EQ(KRPC_ERROR_ARGUMENT, krpc_invoke(connection, &result, &call.message));
}

TEST_F(ErrorsTest, Refused) {
  queue_connection_response(krpc_schema_ConnectionResponse_Status_DENIED, "Not today");
  ASSERT_EQ(KRPC_ERROR_CONNECTION_REFUSED, krpc_connect(connection, "test"));
#ifndef KRPC_ERROR_CODES_ONLY
  EXPECT_EQ(krpc_schema_ConnectionResponse_Status_DENIED, krpc_get_error_info()->status);
  EXPECT_EQ(-1, krpc_get_error_info()->reason);
#endif
#ifdef KRPC_ERROR_MESSAGES
  EXPECT_STREQ("Connection refused by the server: Not today", krpc_get_error_message());
#endif
}

TEST_F(ErrorsTest, Disconnected) {
  connect();
  krpc_schema_Message message = krpc_schema_Message_init_default;
  message.which_content = krpc_schema_Message_disconnect_tag;
  message.content.disconnect = krpc_schema_Disconnect_init_default;
  message.content.disconnect.reason = krpc_schema_Disconnect_Reason_SERVER_STOPPED;
  message.content.disconnect.message.funcs.encode = &krpc_encode_callback_cstring;
  message.content.disconnect.message.arg = const_cast<char*>("Stopping");
  queue(message);

  ASSERT_EQ(KRPC_ERROR_CONNECTION, invoke());
#ifndef KRPC_ERROR_CODES_ONLY
  EXPECT_EQ(krpc_schema_Disconnect_Reason_SERVER_STOPPED, krpc_get_error_info()->reason);
#endif
#ifdef KRPC_ERROR_MESSAGES
  EXPECT_STREQ("Connection ended by the server: Stopping", krpc_get_error_message());
#endif
  // A call after the session has ended fails at once
  ASSERT_EQ(KRPC_ERROR_CONNECTION, invoke());
}

TEST_F(ErrorsTest, TransportFailure) {
  link.write_failure = "Cable cut";
  ASSERT_EQ(KRPC_ERROR_CONNECTION, krpc_connect(connection, "test"));
#ifndef KRPC_ERROR_CODES_ONLY
  EXPECT_EQ(KRPC_ERROR_CONNECTION, krpc_get_error_info()->code);
  EXPECT_EQ(KRPC_ERROR_IO, krpc_get_error_info()->cause);
#endif
#ifdef KRPC_ERROR_MESSAGES
  EXPECT_STREQ("Connection failed: Cable cut", krpc_get_error_message());
#endif
}

TEST_F(ErrorsTest, ConnectionEnds) {
  ASSERT_EQ(KRPC_ERROR_CONNECTION, krpc_connect(connection, "test"));
#ifndef KRPC_ERROR_CODES_ONLY
  EXPECT_EQ(KRPC_ERROR_EOF, krpc_get_error_info()->cause);
#endif
#ifdef KRPC_ERROR_MESSAGES
  // The transport gave no message for its failure
  EXPECT_STREQ("Connection failed", krpc_get_error_message());
#endif
}

#ifdef KRPC_ERROR_MESSAGES
// A message too long for the buffer is cut, keeping the sentence in front
TEST_F(ErrorsTest, LongCause) {
  link.write_failure = std::string(size_t{2} * KRPC_ERROR_MESSAGE_LENGTH, 'x');
  ASSERT_EQ(KRPC_ERROR_CONNECTION, krpc_connect(connection, "test"));
  std::string message = krpc_get_error_message();
  EXPECT_EQ(static_cast<size_t>(KRPC_ERROR_MESSAGE_LENGTH - 1), message.size());
  EXPECT_EQ(0u, message.rfind("Connection failed: xxx", 0));
}
#endif
