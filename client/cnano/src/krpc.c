#include <krpc_cnano.h>
#include <krpc_cnano/encoder.h>
#include <krpc_cnano/pb.h>
#include <krpc_cnano/pb_decode.h>
#include <krpc_cnano/pb_encode.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <string.h>

/* A message is written to the connection through a buffer, rather than in the pieces the
   protocol buffer encoder works in. A piece is a field tag, a length or a single value, so a
   call handed straight to the connection costs tens of writes, each of which is a system call
   and - with the transport asked not to hold data back - a packet of its own.

   The buffer bounds how much is written at a time, never the size of a message: one larger
   than the buffer is written in as many passes as it takes. It is lent by the caller rather
   than held here, so that sending a message costs one buffer whichever way it is written. */
typedef struct {
  krpc_connection_t connection;
  uint8_t *data;
  size_t size;
  size_t used;
} krpc_writer_t;

/* The most room the size a message is prefixed with can need in front of it */
#define KRPC_SIZE_PREFIX_MAX 5

/* Where a message is encoded before it is sent, for the common case of one that fits. Written
   to through a callback of its own rather than by pb_ostream_from_buffer, so that a message too
   big for the buffer is told apart from one that failed to encode: only the first is worth
   encoding a second time to write it a bufferful at a time. */
typedef struct {
  uint8_t *data;
  size_t size;
  size_t used;
  bool overflowed;
} krpc_buffer_t;

/* The same for reading, with one difference: the connection is only ever asked for bytes the
   message being read still has to come, so a read never waits on a message that has not been
   asked for. That is what remaining counts. */
typedef struct {
  krpc_connection_t connection;
  size_t remaining;
  size_t position;
  size_t available;
  uint8_t buffer[KRPC_BUFFER_SIZE];
} krpc_reader_t;

static bool buffer_write_callback(pb_ostream_t *stream, const uint8_t *buf, size_t count);
static bool write_callback(pb_ostream_t *stream, const uint8_t *buf, size_t count);
static bool read_callback(pb_istream_t *stream, uint8_t *buf, size_t count);
static krpc_error_t krpc_send_message(krpc_connection_t connection, const pb_msgdesc_t *fields,
                                      const void *message);
static krpc_error_t krpc_receive_message(krpc_connection_t connection, const pb_msgdesc_t *fields,
                                         void *message);

krpc_error_t krpc_connect(krpc_connection_t connection, const char *client_name) {
  {
    // Send connection request message
#ifdef KRPC_MULTIPLEXED
    krpc_schema_MultiplexedRequest message = krpc_schema_MultiplexedRequest_init_default;
    const pb_msgdesc_t *fields = krpc_schema_MultiplexedRequest_fields;
    krpc_schema_ConnectionRequest *request = &message.connection_request;
    message.has_connection_request = true;
#else
    krpc_schema_ConnectionRequest message = krpc_schema_ConnectionRequest_init_default;
    const pb_msgdesc_t *fields = krpc_schema_ConnectionRequest_fields;
    krpc_schema_ConnectionRequest *request = &message;
#endif
    request->type = krpc_schema_ConnectionRequest_Type_RPC;
    request->client_name.funcs.encode = &krpc_encode_callback_cstring;
    request->client_name.arg = (void *)client_name;
    if (krpc_send_message(connection, fields, &message) != KRPC_OK) {
      krpc_close(connection);
      KRPC_RETURN_ERROR(ENCODING_FAILED, "failed to send connection request");
    }
  }

  {
    // Receive connection response message
    krpc_schema_ConnectionResponse response = krpc_schema_ConnectionResponse_init_default;
    if (krpc_receive_message(connection, krpc_schema_ConnectionResponse_fields, &response) !=
        KRPC_OK) {
      krpc_close(connection);
      KRPC_RETURN_ERROR(DECODING_FAILED, "failed to receive connection response");
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
    // Create request message containing the procedure call
#ifdef KRPC_MULTIPLEXED
    krpc_schema_MultiplexedRequest message = krpc_schema_MultiplexedRequest_init_default;
    const pb_msgdesc_t *fields = krpc_schema_MultiplexedRequest_fields;
    krpc_schema_Request *request = &message.request;
    message.has_request = true;
#else
    krpc_schema_Request message = krpc_schema_Request_init_default;
    const pb_msgdesc_t *fields = krpc_schema_Request_fields;
    krpc_schema_Request *request = &message;
#endif
    request->calls[0] = *call;
    request->calls_count = 1;

    // Send request message
    KRPC_RETURN_ON_ERROR(krpc_send_message(connection, fields, &message));
  }

  {
    // Receive response message
#ifdef KRPC_MULTIPLEXED
    krpc_schema_MultiplexedResponse message = krpc_schema_MultiplexedResponse_init_default;
    const pb_msgdesc_t *fields = krpc_schema_MultiplexedResponse_fields;
    krpc_schema_Response *response = &message.response;
#else
    krpc_schema_Response message = krpc_schema_Response_init_default;
    const pb_msgdesc_t *fields = krpc_schema_Response_fields;
    krpc_schema_Response *response = &message;
#endif

    response->results[0] = *result;

    krpc_error_t rpc_error = KRPC_OK;
    response->error.funcs.decode = &krpc_decode_callback_error;
    response->error.arg = &rpc_error;
    response->results[0].error.funcs.decode = &krpc_decode_callback_error;
    response->results[0].error.arg = &rpc_error;

    KRPC_RETURN_ON_ERROR(krpc_receive_message(connection, fields, &message));

    if (rpc_error != KRPC_OK) {
#ifdef KRPC_ERROR_MESSAGES
      KRPC_RETURN_ERROR(RPC_FAILED, krpc_get_error_message());
#else
      KRPC_RETURN_ERROR(RPC_FAILED, "rpc returned an error");
#endif
    }

    // Extract the procedure result message from the response
    if (response->results_count != 1)
      KRPC_RETURN_ERROR(NO_RESULTS, "response message does not contain a single result");
    *result = response->results[0];
  }
  return KRPC_OK;
}

static bool buffer_write_callback(pb_ostream_t *stream, const uint8_t *buf, size_t count) {
  krpc_buffer_t *buffer = (krpc_buffer_t *)stream->state;
  if (count > buffer->size - buffer->used) {
    buffer->overflowed = true;
    return false;
  }
  memcpy(buffer->data + buffer->used, buf, count);
  buffer->used += count;
  return true;
}

/* Hand what has been buffered to the connection */
static krpc_error_t krpc_writer_flush(krpc_writer_t *writer) {
  size_t used = writer->used;
  writer->used = 0;
  if (used == 0) return KRPC_OK;
  return krpc_write(writer->connection, writer->data, used);
}

static bool write_callback(pb_ostream_t *stream, const uint8_t *buf, size_t count) {
  krpc_writer_t *writer = (krpc_writer_t *)stream->state;
  while (count > 0) {
    size_t space;
    size_t take;
    if (writer->used == writer->size) KRPC_CALLBACK_RETURN_ON_ERROR(krpc_writer_flush(writer));
    space = writer->size - writer->used;
    take = count < space ? count : space;
    memcpy(writer->data + writer->used, buf, take);
    writer->used += take;
    buf += take;
    count -= take;
  }
  return true;
}

/* Ask the connection for as much of what the message has left to come as the buffer will hold */
static krpc_error_t krpc_reader_fill(krpc_reader_t *reader) {
  size_t wanted = reader->remaining;
  if (wanted == 0) KRPC_RETURN_ERROR(EOF, "read past the end of the message");
  if (wanted > sizeof(reader->buffer)) wanted = sizeof(reader->buffer);
  KRPC_RETURN_ON_ERROR(krpc_read(reader->connection, reader->buffer, wanted));
  reader->position = 0;
  reader->available = wanted;
  reader->remaining -= wanted;
  return KRPC_OK;
}

static bool read_callback(pb_istream_t *stream, uint8_t *buf, size_t count) {
  krpc_reader_t *reader = (krpc_reader_t *)stream->state;
  while (count > 0) {
    size_t take;
    if (reader->position == reader->available) {
      krpc_error_t error = krpc_reader_fill(reader);
      if (error == KRPC_ERROR_EOF) stream->bytes_left = 0;
      KRPC_CALLBACK_RETURN_ON_ERROR(error);
    }
    take = reader->available - reader->position;
    if (take > count) take = count;
    memcpy(buf, reader->buffer + reader->position, take);
    reader->position += take;
    buf += take;
    count -= take;
  }
  return true;
}

/* The size a message is prefixed with, read a byte at a time as it is the only part of a
   message whose length is not known before it has been read. Knowing the size is what lets
   the rest of the message be read in whole buffers. */
static krpc_error_t krpc_read_size(krpc_connection_t connection, size_t *size) {
  uint8_t byte;
  size_t result = 0;
  unsigned int shift = 0;
  do {
    if (shift > sizeof(size_t) * 8 - 7) KRPC_RETURN_ERROR(DECODING_FAILED, "message size too big");
    KRPC_RETURN_ON_ERROR(krpc_read(connection, &byte, 1));
    result |= (size_t)(byte & 0x7F) << shift;
    shift += 7;
  } while (byte & 0x80);
  *size = result;
  return KRPC_OK;
}

/* How many bytes the size a message is prefixed with takes to write */
static size_t krpc_size_prefix_length(size_t size) {
  size_t length = 1;
  while (size >= 0x80) {
    size >>= 7;
    length++;
  }
  return length;
}

static krpc_error_t krpc_send_message(krpc_connection_t connection, const pb_msgdesc_t *fields,
                                      const void *message) {
  uint8_t data[KRPC_BUFFER_SIZE];
  krpc_buffer_t buffer;
  pb_ostream_t stream = {&buffer_write_callback, NULL, SIZE_MAX, 0};
  buffer.data = data + KRPC_SIZE_PREFIX_MAX;
  buffer.size = sizeof(data) - KRPC_SIZE_PREFIX_MAX;
  buffer.used = 0;
  buffer.overflowed = false;
  stream.state = &buffer;
  /* A message is prefixed with its size, so writing one in order means knowing how long it is
     before it has been encoded, which costs a pass over it to measure. Encoding it into the
     buffer with room left in front instead means the size is known once it is there, and can
     be written into the room left for it. That halves what encoding a message costs. */
  if (pb_encode(&stream, fields, message)) {
    size_t size = buffer.used;
    size_t length = krpc_size_prefix_length(size);
    uint8_t *at = buffer.data - length;
    uint8_t *start = at;
    while (size >= 0x80) {
      *at++ = (uint8_t)(size | 0x80);
      size >>= 7;
    }
    *at = (uint8_t)size;
    return krpc_write(connection, start, length + buffer.used);
  }
  /* A message that failed to encode for any other reason would only fail the same way again,
     so it is reported here rather than encoded a second time. */
  if (!buffer.overflowed)
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode message", &stream);
  /* Larger than the buffer, so it is measured and then written a bufferful at a time, through
     the same buffer the message did not fit in. */
  {
    krpc_writer_t writer;
    pb_ostream_t streamed = {&write_callback, NULL, SIZE_MAX, 0};
    writer.connection = connection;
    writer.data = data;
    writer.size = sizeof(data);
    writer.used = 0;
    streamed.state = &writer;
    if (!pb_encode_delimited(&streamed, fields, message))
      KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode message", &streamed);
    return krpc_writer_flush(&writer);
  }
}

static krpc_error_t krpc_receive_message(krpc_connection_t connection, const pb_msgdesc_t *fields,
                                         void *message) {
  krpc_reader_t reader;
  pb_istream_t stream = {&read_callback, NULL, 0};
  size_t size;
  KRPC_RETURN_ON_ERROR(krpc_read_size(connection, &size));
  reader.connection = connection;
  reader.remaining = size;
  reader.position = 0;
  reader.available = 0;
  stream.state = &reader;
  stream.bytes_left = size;
  if (!pb_decode(&stream, fields, message))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode message", &stream);
  return KRPC_OK;
}
