#include <krpc_cnano/communication.h>
#include <krpc_cnano/error.h>

#if defined(KRPC_PLATFORM_POSIX)

#include <fcntl.h>
#include <stdint.h>
#include <unistd.h>

static krpc_error_t krpc_serial_read(uintptr_t handle, uint8_t* buf, size_t count) {
  size_t total = 0;
  while (total < count) {
    ssize_t result = read((int)handle, buf + total, count - total);
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

static krpc_error_t krpc_serial_write(uintptr_t handle, const uint8_t* buf, size_t count) {
  if ((ssize_t)count != write((int)handle, buf, count)) KRPC_RETURN_ERROR(IO, "write failed");
  return KRPC_OK;
}

static krpc_error_t krpc_serial_close(uintptr_t handle) {
  close((int)handle);
  return KRPC_OK;
}

const krpc_transport_t krpc_transport_serial = {&krpc_serial_read, &krpc_serial_write,
                                                &krpc_serial_close, true};

krpc_error_t krpc_open_serial(krpc_connection_t* connection, const char* port) {
  int fd;
  connection->transport = NULL;
  connection->handle = 0;
  if (port == NULL) KRPC_RETURN_ERROR(IO, "no serial port to open");
  fd = open(port, O_RDWR | O_NOCTTY);
  if (fd < 0) KRPC_RETURN_ERROR(IO, "failed to open serial port");
  connection->transport = &krpc_transport_serial;
  connection->handle = (uintptr_t)fd;
  return KRPC_OK;
}

#endif

#if defined(KRPC_PLATFORM_WINDOWS)

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>

static krpc_error_t krpc_serial_read(uintptr_t handle, uint8_t* buf, size_t count) {
  size_t total = 0;
  while (total < count) {
    DWORD bytes_read = 0;
    if (!ReadFile((HANDLE)handle, buf + total, (DWORD)(count - total), &bytes_read, NULL))
      KRPC_RETURN_ERROR(IO, "read failed");
    if (bytes_read == 0) KRPC_RETURN_ERROR(EOF, "eof received");
    total += bytes_read;
  }
  return KRPC_OK;
}

static krpc_error_t krpc_serial_write(uintptr_t handle, const uint8_t* buf, size_t count) {
  DWORD bytes_written = 0;
  if (!WriteFile((HANDLE)handle, buf, (DWORD)count, &bytes_written, NULL) ||
      bytes_written != (DWORD)count)
    KRPC_RETURN_ERROR(IO, "write failed");
  return KRPC_OK;
}

static krpc_error_t krpc_serial_close(uintptr_t handle) {
  CloseHandle((HANDLE)handle);
  return KRPC_OK;
}

const krpc_transport_t krpc_transport_serial = {&krpc_serial_read, &krpc_serial_write,
                                                &krpc_serial_close, true};

krpc_error_t krpc_open_serial(krpc_connection_t* connection, const char* port) {
  HANDLE handle;
  connection->transport = NULL;
  connection->handle = 0;
  if (port == NULL) KRPC_RETURN_ERROR(IO, "no serial port to open");
  handle = CreateFileA(port, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING,
                       FILE_ATTRIBUTE_NORMAL, NULL);
  if (handle == INVALID_HANDLE_VALUE) KRPC_RETURN_ERROR(IO, "failed to open serial port");
  connection->transport = &krpc_transport_serial;
  connection->handle = (uintptr_t)handle;
  return KRPC_OK;
}

#endif
