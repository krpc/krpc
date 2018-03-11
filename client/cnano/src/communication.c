#include <krpc/communication.h>

#include <krpc/error.h>

#if defined(KRPC_COMMUNICATION_POSIX)

#include <fcntl.h>
#include <stdint.h>
#include <unistd.h>

krpc_error_t krpc_open(krpc_connection_t * connection, const void * arg) {
  const char * port = (const char *)arg;
  int fd = open(port, O_RDWR | O_NOCTTY);
  if (fd < 0)
    KRPC_RETURN_ERROR(IO, "failed to open serial port");
  *connection = fd;
  return KRPC_OK;
}

krpc_error_t krpc_close(krpc_connection_t connection) {
  close(connection);
  return KRPC_OK;
}

krpc_error_t krpc_read(krpc_connection_t connection, uint8_t * buf, size_t count) {
  size_t total = 0;
  while (total < count) {
    int result = read(connection, buf, count);
    if (result == -1) {
      KRPC_RETURN_ERROR(IO, "read failed");
    } else if (result == 0) {
      KRPC_RETURN_ERROR(EOF, "eof received");
    } else {
      total += result;
    }
  }
  return KRPC_OK;
}

krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t * buf, size_t count) {
  if (count != write(connection, buf, count))
    KRPC_RETURN_ERROR(IO, "write failed");
  return KRPC_OK;
}

#endif

#if defined(KRPC_COMMUNICATION_ARDUINO)

krpc_error_t krpc_open(krpc_connection_t * connection, const void * arg) {
  (*connection)->begin(9600);
  while (!*connection) {
  }
  return KRPC_OK;
}

krpc_error_t krpc_close(krpc_connection_t connection) {
  connection->end();
  return KRPC_OK;
}

krpc_error_t krpc_read(krpc_connection_t connection, uint8_t * buf, size_t count) {
  size_t read = 0;
  while (true) {
    read += connection->readBytes(buf+read, count-read);
    if (read == count)
      return KRPC_OK;
    if (read > count)
      KRPC_RETURN_ERROR(IO, "read failed");
  }
}

krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t * buf, size_t count) {
  if (count != connection->write(buf, count))
    KRPC_RETURN_ERROR(IO, "write failed");
  return KRPC_OK;
}

#endif  // KRPC_PLATFORM_ARDUINO
