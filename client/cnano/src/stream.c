#include <krpc_cnano/communication.h>
#include <krpc_cnano/error.h>

#if defined(KRPC_PLATFORM_ARDUINO)

static krpc_error_t krpc_stream_read(uintptr_t handle, uint8_t* buf, size_t count) {
  Stream* stream = (Stream*)handle;
  size_t total = 0;
  while (total < count) {
    // A serial link has no end-of-file, so the only sign the peer has gone is silence.
    // readBytes waits up to the stream timeout for each byte, so it returns nothing only when
    // not a single byte arrived in that time. Stream::setTimeout sets how long that is.
    size_t result = stream->readBytes(buf + total, count - total);
    if (result == 0) KRPC_TRANSPORT_FAIL(EOF, "No data before the stream timeout");
    total += result;
  }
  return KRPC_OK;
}

static krpc_error_t krpc_stream_write(uintptr_t handle, const uint8_t* buf, size_t count) {
  Stream* stream = (Stream*)handle;
  if (count != stream->write(buf, count)) KRPC_TRANSPORT_FAIL(IO, "Write failed");
  return KRPC_OK;
}

// A Stream has no end, so closing the connection leaves the port as the sketch started it
const krpc_transport_t krpc_transport_stream = {&krpc_stream_read, &krpc_stream_write, NULL, true};

krpc_error_t krpc_open_stream(krpc_connection_t* connection, Stream* stream) {
  connection->transport = NULL;
  connection->handle = 0;
  if (stream == NULL) KRPC_FAIL(ARGUMENT, "No stream to open");
  connection->transport = &krpc_transport_stream;
  connection->handle = (uintptr_t)stream;
  return KRPC_OK;
}

#endif
