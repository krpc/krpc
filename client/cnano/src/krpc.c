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

#define KRPC_PROTOCOL_VERSION 2

/* Session state is held in file statics, as a connection handle carries no state of its own.
   krpc_connect resets it, so the client supports one open connection at a time. */

/* The id of the next request, which starts at 1 in each session */
static uint64_t krpc_next_id = 1;

/* Whether the session has ended, after which a call fails without touching the connection */
static bool krpc_session_ended = false;

#ifdef KRPC_RESUMABLE
/* Whether no response has been read since the handshake. Until one has, a connection response
   on the line answers a connection request that queued while the server was not reading. */
static bool krpc_awaiting_first_response = true;
#endif

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

#ifdef KRPC_WRITE_FAULTS
krpc_error_t (*krpc_write_fault_hook)(void) = NULL;
#endif

/* Write to the connection, unless the testee's write fault fails the write first */
static krpc_error_t krpc_write_to(krpc_connection_t connection, const uint8_t *buf, size_t count) {
#ifdef KRPC_WRITE_FAULTS
  if (krpc_write_fault_hook != NULL) {
    krpc_error_t error = krpc_write_fault_hook();
    if (error != KRPC_OK) return error;
  }
#endif
  return krpc_write(connection, buf, count);
}
static krpc_error_t krpc_receive_message(krpc_connection_t connection, const pb_msgdesc_t *fields,
                                         void *message);

/* Send a Message holding a disconnect with the given reason */
static krpc_error_t krpc_send_disconnect(krpc_connection_t connection,
                                         krpc_schema_Disconnect_Reason reason) {
  krpc_schema_Message message = krpc_schema_Message_init_default;
  krpc_schema_Disconnect disconnect = krpc_schema_Disconnect_init_default;
  disconnect.reason = reason;
  message.which_content = krpc_schema_Message_disconnect_tag;
  message.content.disconnect = disconnect;
  return krpc_send_message(connection, krpc_schema_Message_fields, &message);
}

/* End the session on a message the server should not have sent */
static krpc_error_t krpc_protocol_error(krpc_connection_t connection) {
  krpc_send_disconnect(connection, krpc_schema_Disconnect_Reason_PROTOCOL_ERROR);
  krpc_session_ended = true;
  KRPC_RETURN_ERROR(CONNECTION_FAILED, "unexpected message from the server");
}

/* End the session on a message that fails to parse */
static krpc_error_t krpc_malformed_message(krpc_connection_t connection) {
  krpc_send_disconnect(connection, krpc_schema_Disconnect_Reason_MALFORMED_MESSAGE);
  krpc_session_ended = true;
  KRPC_RETURN_ERROR(CONNECTION_FAILED, "failed to parse a message from the server");
}

/* Close a connection whose handshake failed. No session started, so krpc_disconnect has none to
   end. */
static void krpc_close_failed(krpc_connection_t connection) {
  krpc_session_ended = true;
  krpc_close(connection);
}

krpc_error_t krpc_connect(krpc_connection_t connection, const char *client_name) {
  krpc_next_id = 1;
  krpc_session_ended = false;
#ifdef KRPC_RESUMABLE
  krpc_awaiting_first_response = true;
#endif
  {
    // Send connection request message
    krpc_schema_Message message = krpc_schema_Message_init_default;
    krpc_schema_ConnectionRequest *request = &message.content.connection_request;
    message.which_content = krpc_schema_Message_connection_request_tag;
    request->protocol_version = KRPC_PROTOCOL_VERSION;
    request->client_name.funcs.encode = &krpc_encode_callback_cstring;
    request->client_name.arg = (void *)client_name;
    if (krpc_send_message(connection, krpc_schema_Message_fields, &message) != KRPC_OK) {
      krpc_close_failed(connection);
      KRPC_RETURN_ERROR(ENCODING_FAILED, "failed to send connection request");
    }
  }

  while (true) {
    // Receive connection response message
    krpc_schema_Message message = krpc_schema_Message_init_default;
    krpc_schema_ConnectionResponse *response = &message.content.connection_response;
    if (krpc_receive_message(connection, krpc_schema_Message_fields, &message) != KRPC_OK) {
#ifdef KRPC_RESUMABLE
      // Part of an earlier session's message, read as one of its own. The line stays open for
      // the program to retry.
      KRPC_RETURN_ERROR(CONNECTION_FAILED, "failed to receive connection response, retry");
#else
      krpc_close_failed(connection);
      KRPC_RETURN_ERROR(CONNECTION_FAILED, "failed to receive connection response");
#endif
    }
    if (message.which_content != krpc_schema_Message_connection_response_tag) {
#ifdef KRPC_RESUMABLE
      // A disconnect ends the session the request landed in, such as part of an earlier
      // session's message. The server then waits for a new request.
      if (message.which_content == krpc_schema_Message_disconnect_tag)
        KRPC_RETURN_ERROR(CONNECTION_FAILED, "connection ended by server, retry");
      // A message left on the line by an earlier session
      continue;
#else
      krpc_close_failed(connection);
      if (message.which_content == krpc_schema_Message_disconnect_tag)
        KRPC_RETURN_ERROR(CONNECTION_FAILED, "connection ended by server");
      KRPC_RETURN_ERROR(CONNECTION_FAILED, "expected a response to the connection request");
#endif
    }

    // Check the connection status
    if (response->status == krpc_schema_ConnectionResponse_Status_DENIED) {
      krpc_close_failed(connection);
      KRPC_RETURN_ERROR(CONNECTION_REFUSED, "connection denied by server");
    }
    if (response->status == krpc_schema_ConnectionResponse_Status_WRONG_PROTOCOL_VERSION) {
      krpc_close_failed(connection);
      KRPC_RETURN_ERROR(CONNECTION_REFUSED,
                        "server does not support the client's protocol version");
    }
    if (response->status != krpc_schema_ConnectionResponse_Status_OK) {
#ifdef KRPC_RESUMABLE
      // The server read part of an earlier session's message, or nothing, as the request. The
      // line stays open for the program to retry.
      KRPC_RETURN_ERROR(CONNECTION_FAILED, "connection request was not read, retry");
#else
      krpc_close_failed(connection);
      KRPC_RETURN_ERROR(CONNECTION_FAILED, "connection refused by server");
#endif
    }
    if (response->protocol_version != KRPC_PROTOCOL_VERSION) {
      krpc_close_failed(connection);
      KRPC_RETURN_ERROR(CONNECTION_REFUSED,
                        "server does not support the client's protocol version");
    }
    return KRPC_OK;
  }
}

krpc_error_t krpc_disconnect(krpc_connection_t connection) {
  if (!krpc_session_ended) krpc_send_disconnect(connection, krpc_schema_Disconnect_Reason_CLOSED);
  krpc_session_ended = true;
  return krpc_close(connection);
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

/* What the content callback of a response needs: the result to copy in, which carries the decode
   callback for its value, and the error the decode callbacks for an error report to */
typedef struct {
  const krpc_schema_ProcedureResult *result;
  krpc_error_t *rpc_error;
} krpc_response_decoding_t;

/* Set up the decode callbacks of a response once nanopb has started decoding it. Decoding the
   tag of a case of a Message clears the case's fields, which would discard callbacks set any
   earlier. */
static bool krpc_decode_callback_content(pb_istream_t *stream, const pb_field_t *field,
                                         void **arg) {
  (void)stream;
  if (field->tag == krpc_schema_Message_response_tag) {
    const krpc_response_decoding_t *decoding = (const krpc_response_decoding_t *)(*arg);
    krpc_schema_Response *response = (krpc_schema_Response *)field->pData;
    response->results[0] = *decoding->result;
    response->error.funcs.decode = &krpc_decode_callback_error;
    response->error.arg = decoding->rpc_error;
    response->results[0].error.funcs.decode = &krpc_decode_callback_error;
    response->results[0].error.arg = decoding->rpc_error;
  }
  return true;
}

krpc_error_t krpc_invoke(krpc_connection_t connection, krpc_schema_ProcedureResult *result,
                         krpc_schema_ProcedureCall *call) {
  uint64_t id;
  if (krpc_session_ended) KRPC_RETURN_ERROR(CONNECTION_FAILED, "connection ended");
  id = krpc_next_id++;
  {
    // Create request message containing the procedure call
    krpc_schema_Message message = krpc_schema_Message_init_default;
    krpc_schema_Request *request = &message.content.request;
    message.which_content = krpc_schema_Message_request_tag;
    request->id = id;
    request->calls[0] = *call;
    request->calls_count = 1;

    // Send request message. A failed write ends the session.
    {
      krpc_error_t error = krpc_send_message(connection, krpc_schema_Message_fields, &message);
      if (error != KRPC_OK) {
        krpc_session_ended = true;
        return error;
      }
    }
  }

  while (true) {
    // Receive response message
    krpc_schema_Message message = krpc_schema_Message_init_default;
    krpc_schema_Response *response = &message.content.response;
    krpc_error_t rpc_error = KRPC_OK;
    krpc_response_decoding_t decoding;
    decoding.result = result;
    decoding.rpc_error = &rpc_error;
    message.cb_content.funcs.decode = &krpc_decode_callback_content;
    message.cb_content.arg = &decoding;

    {
      krpc_error_t error = krpc_receive_message(connection, krpc_schema_Message_fields, &message);
      if (error == KRPC_ERROR_DECODING_FAILED) return krpc_malformed_message(connection);
      // A transport that fails, or ends, ends the session
      if (error != KRPC_OK) {
        krpc_session_ended = true;
        return error;
      }
    }

    switch (message.which_content) {
      case krpc_schema_Message_response_tag:
        break;
      case krpc_schema_Message_disconnect_tag:
        krpc_session_ended = true;
        KRPC_RETURN_ERROR(CONNECTION_FAILED, "connection ended by server");
#ifdef KRPC_RESUMABLE
      case krpc_schema_Message_connection_response_tag:
        // Each connection request that queued while the server was not reading starts a new
        // session, and its OK replaces the current one
        if (krpc_awaiting_first_response) {
          if (message.content.connection_response.status !=
              krpc_schema_ConnectionResponse_Status_OK) {
            krpc_session_ended = true;
            KRPC_RETURN_ERROR(CONNECTION_FAILED, "connection refused by server");
          }
          continue;
        }
        return krpc_protocol_error(connection);
#endif
      case 0:
        // A message of a kind this client does not know is skipped
        continue;
      default:
        return krpc_protocol_error(connection);
    }

    if (response->id != id) return krpc_protocol_error(connection);
#ifdef KRPC_RESUMABLE
    krpc_awaiting_first_response = false;
#endif

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
    return KRPC_OK;
  }
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
  return krpc_write_to(writer->connection, writer->data, used);
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
  uint32_t result = 0;
  unsigned int shift = 0;
  do {
    // A size is 32 bits, so five bytes carry the whole of it, the fifth its top four bits
    if (shift >= 35) KRPC_RETURN_ERROR(DECODING_FAILED, "message size too big");
    KRPC_RETURN_ON_ERROR(krpc_read(connection, &byte, 1));
    if (shift == 28 && (byte & 0x70)) KRPC_RETURN_ERROR(DECODING_FAILED, "message size too big");
    result |= (uint32_t)(byte & 0x7F) << shift;
    shift += 7;
  } while (byte & 0x80);
#if SIZE_MAX < UINT32_MAX
  if (result > SIZE_MAX) KRPC_RETURN_ERROR(DECODING_FAILED, "message size too big");
#endif
  *size = (size_t)result;
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
    return krpc_write_to(connection, start, length + buffer.used);
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
