#include <krpc_cnano/communication.h>

#include <krpc_cnano/error.h>

#if defined(KRPC_COMMUNICATION_POSIX)

#include <fcntl.h>
#include <stdint.h>
#include <unistd.h>

krpc_error_t krpc_open(krpc_connection_t * connection, const krpc_connection_config_t * arg) {
  const char * port = arg;
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

#if defined(KRPC_COMMUNICATION_WINDOWS)

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>

krpc_error_t krpc_open(krpc_connection_t * connection, const krpc_connection_config_t * arg) {
  const char * port = arg;
  HANDLE handle = CreateFileA(port, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
  if (handle == INVALID_HANDLE_VALUE)
    KRPC_RETURN_ERROR(IO, "failed to open serial port");
  *connection = handle;
  return KRPC_OK;
}

krpc_error_t krpc_close(krpc_connection_t connection) {
  CloseHandle((HANDLE)connection);
  return KRPC_OK;
}

krpc_error_t krpc_read(krpc_connection_t connection, uint8_t * buf, size_t count) {
  size_t total = 0;
  while (total < count) {
    DWORD bytes_read = 0;
    if (!ReadFile((HANDLE)connection, buf + total, (DWORD)(count - total), &bytes_read, NULL))
      KRPC_RETURN_ERROR(IO, "read failed");
    if (bytes_read == 0)
      KRPC_RETURN_ERROR(EOF, "eof received");
    total += bytes_read;
  }
  return KRPC_OK;
}

krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t * buf, size_t count) {
  DWORD bytes_written = 0;
  if (!WriteFile((HANDLE)connection, buf, (DWORD)count, &bytes_written, NULL) || bytes_written != (DWORD)count)
    KRPC_RETURN_ERROR(IO, "write failed");
  return KRPC_OK;
}

#endif

#if defined(KRPC_COMMUNICATION_ARDUINO)

krpc_error_t krpc_open(krpc_connection_t * connection, const krpc_connection_config_t * arg) {
  if (arg)
    (*connection)->begin(arg->speed, arg->config);
  else
    (*connection)->begin(9600, SERIAL_8N1);
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
  }
}

krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t * buf, size_t count) {
  if (count != connection->write(buf, count))
    KRPC_RETURN_ERROR(IO, "write failed");
  return KRPC_OK;
}

#endif  // KRPC_PLATFORM_ARDUINO
