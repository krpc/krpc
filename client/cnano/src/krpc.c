#include <krpc_cnano.h>
#include <krpc_cnano/encoder.h>
#include <krpc_cnano/pb.h>
#include <krpc_cnano/pb_decode.h>
#include <krpc_cnano/pb_encode.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#ifdef KRPC_ERROR_MESSAGES
#include <string.h>
#endif

static bool write_callback(pb_ostream_t *stream, const uint8_t *buf, size_t count);
static bool read_callback(pb_istream_t *stream, uint8_t *buf, size_t count);
static pb_ostream_t krpc_pb_ostream_from_connection(krpc_connection_t connection);
static pb_istream_t krpc_pb_istream_from_connection(krpc_connection_t connection);

krpc_error_t krpc_connect(krpc_connection_t connection, const char *client_name) {
  {
    // Send connection request message
    krpc_schema_MultiplexedRequest request = krpc_schema_MultiplexedRequest_init_default;
    request.has_connection_request = true;
    request.connection_request.type = krpc_schema_ConnectionRequest_Type_RPC;
    request.connection_request.client_name.funcs.encode = &krpc_encode_callback_cstring;
    request.connection_request.client_name.arg = (void *)client_name;
    pb_ostream_t stream = krpc_pb_ostream_from_connection(connection);
    if (!pb_encode_delimited(&stream, krpc_schema_MultiplexedRequest_fields, &request)) {
      krpc_close(connection);
      KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode connection request", &stream);
    }
  }

  {
    // Receive connection response message
    pb_istream_t stream = krpc_pb_istream_from_connection(connection);
    krpc_schema_ConnectionResponse response = krpc_schema_ConnectionResponse_init_default;
    if (!pb_decode_delimited(&stream, krpc_schema_ConnectionResponse_fields, &response)) {
      krpc_close(connection);
      KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode connection response", &stream);
    }

    // Check the connection status
    if (response.status != krpc_schema_ConnectionResponse_Status_OK) {
      krpc_close(connection);
      KRPC_RETURN_ERROR(CONNECTION_FAILED, "connection denied by server");
    }
  }
  return KRPC_OK;
}

#ifdef KRPC_ERROR_MESSAGES

static char krpc_error_message[KRPC_ERROR_MESSAGE_LENGTH];

const char *krpc_get_error_message(void) { return krpc_error_message; }

/* Append to the error message, discarding whatever does not fit */
static void krpc_append_error_message(const char *str, size_t length) {
  size_t used = strlen(krpc_error_message);
  size_t space = KRPC_ERROR_MESSAGE_LENGTH - used - 1;
  if (length > space) length = space;
  memcpy(krpc_error_message + used, str, length);
  krpc_error_message[used + length] = '\0';
}

/* Decode one of the string fields of an error message onto the end of the error message,
 * preceded by the separator passed as arg. The separator is skipped when nothing has been
 * appended yet, so that fields the server left empty do not leave stray punctuation behind.
 * The string is copied in chunks to avoid needing a buffer as large as the field. */
static bool krpc_decode_callback_error_string(pb_istream_t *stream, const pb_field_t *field,
                                              void **arg) {
  (void)field;
  if (stream->bytes_left == 0) return true;
  if (krpc_error_message[0] != '\0') {
    const char *separator = (const char *)(*arg);
    krpc_append_error_message(separator, strlen(separator));
  }
  char chunk[32];
  while (stream->bytes_left > 0) {
    size_t size = stream->bytes_left < sizeof(chunk) ? stream->bytes_left : sizeof(chunk);
    if (!pb_read(stream, (pb_byte_t *)chunk, size))
      KRPC_CALLBACK_RETURN_STREAM_ERROR("failed to decode error message field", stream);
    krpc_append_error_message(chunk, size);
  }
  return true;
}

#endif

static bool krpc_decode_callback_error(pb_istream_t *stream, const pb_field_t *field, void **arg) {
  krpc_error_t *error_code = (krpc_error_t *)(*arg);
  *error_code = KRPC_ERROR_RPC_FAILED;
  krpc_schema_Error error = krpc_schema_Error_init_default;
#ifdef KRPC_ERROR_MESSAGES
  krpc_error_message[0] = '\0';
  error.service.funcs.decode = &krpc_decode_callback_error_string;
  error.service.arg = (void *)"";
  error.name.funcs.decode = &krpc_decode_callback_error_string;
  error.name.arg = (void *)".";
  error.description.funcs.decode = &krpc_decode_callback_error_string;
  error.description.arg = (void *)": ";
  error.stack_trace.funcs.decode = &krpc_decode_callback_error_string;
  error.stack_trace.arg = (void *)"\nServer stack trace:\n";
#endif
  if (!pb_decode(stream, krpc_schema_Error_fields, &error))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode error message", stream);
  return true;
}

krpc_error_t krpc_invoke(krpc_connection_t connection, krpc_schema_ProcedureResult *result,
                         krpc_schema_ProcedureCall *call) {
  {
    pb_ostream_t ostream = krpc_pb_ostream_from_connection(connection);

    // Create request message containing the procedure call
    krpc_schema_MultiplexedRequest m_request = krpc_schema_MultiplexedRequest_init_default;
    m_request.has_request = true;
    m_request.request.calls[0] = *call;
    m_request.request.calls_count = 1;

    // Send request message
    if (!pb_encode_delimited(&ostream, krpc_schema_MultiplexedRequest_fields, &m_request))
      KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode request message", &ostream);
  }

  {
    pb_istream_t istream = krpc_pb_istream_from_connection(connection);

    // Receive response message
    krpc_schema_MultiplexedResponse m_response = krpc_schema_MultiplexedResponse_init_default;

    m_response.response.results[0] = *result;

    krpc_error_t rpc_error = KRPC_OK;
    m_response.response.error.funcs.decode = &krpc_decode_callback_error;
    m_response.response.error.arg = &rpc_error;
    m_response.response.results[0].error.funcs.decode = &krpc_decode_callback_error;
    m_response.response.results[0].error.arg = &rpc_error;

    if (!pb_decode_delimited(&istream, krpc_schema_MultiplexedResponse_fields, &m_response))
      KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode response message", &istream);

    if (rpc_error != KRPC_OK) {
#ifdef KRPC_ERROR_MESSAGES
      KRPC_RETURN_ERROR(RPC_FAILED, krpc_get_error_message());
#else
      KRPC_RETURN_ERROR(RPC_FAILED, "rpc returned an error");
#endif
    }

    // Extract the procedure result message from the response
    krpc_schema_Response *response = &m_response.response;
    if (response->results_count != 1)
      KRPC_RETURN_ERROR(NO_RESULTS, "response message does not contain a single result");
    *result = response->results[0];
  }
  return KRPC_OK;
}

static bool write_callback(pb_ostream_t *stream, const uint8_t *buf, size_t count) {
  krpc_connection_t connection = (krpc_connection_t)(intptr_t)stream->state;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_write(connection, buf, count))
  return true;
}

static bool read_callback(pb_istream_t *stream, uint8_t *buf, size_t count) {
  krpc_connection_t connection = (krpc_connection_t)(intptr_t)stream->state;
  krpc_error_t result = krpc_read(connection, buf, count);
  if (result == KRPC_ERROR_EOF) stream->bytes_left = 0;
  KRPC_CALLBACK_RETURN_ON_ERROR(result);
  return true;
}

static pb_ostream_t krpc_pb_ostream_from_connection(krpc_connection_t connection) {
  pb_ostream_t stream = {&write_callback, (void *)(intptr_t)connection, SIZE_MAX, 0};
  return stream;
}

static pb_istream_t krpc_pb_istream_from_connection(krpc_connection_t connection) {
  pb_istream_t stream = {&read_callback, (void *)(intptr_t)connection, SIZE_MAX};
  return stream;
}
