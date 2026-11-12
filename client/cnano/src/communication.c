/* strerror is POSIX's, hidden by a compiler asked for strict C */
#if !defined(ARDUINO) && !defined(_WIN32) && !defined(_POSIX_C_SOURCE)
#define _POSIX_C_SOURCE 200112L
#endif

#include <krpc_cnano/communication.h>

#if defined(KRPC_PLATFORM_WINDOWS) && defined(KRPC_ERROR_MESSAGES)
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>
#elif defined(KRPC_PLATFORM_POSIX) && defined(KRPC_ERROR_MESSAGES)
#include <errno.h>
#include <string.h>
#endif

krpc_error_t krpc_read(krpc_connection_t connection, uint8_t* buf, size_t count) {
  if (connection.transport == NULL) KRPC_TRANSPORT_FAIL(IO, "Connection is not open");
  return connection.transport->read(connection.handle, buf, count);
}

krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t* buf, size_t count) {
  if (connection.transport == NULL) KRPC_TRANSPORT_FAIL(IO, "Connection is not open");
  return connection.transport->write(connection.handle, buf, count);
}

krpc_error_t krpc_close(krpc_connection_t connection) {
  if (connection.transport == NULL) KRPC_TRANSPORT_FAIL(IO, "Connection is not open");
  if (connection.transport->close == NULL) return KRPC_OK;
  return connection.transport->close(connection.handle);
}

#if defined(KRPC_PLATFORM_POSIX) || defined(KRPC_PLATFORM_WINDOWS)

void krpc_set_system_error_cause(void) {
#if defined(KRPC_ERROR_MESSAGES) && defined(KRPC_PLATFORM_WINDOWS)
  char message[128];
  DWORD length = FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL,
                                GetLastError(), 0, message, sizeof(message), NULL);
  // The system's message ends with a period and a line break
  while (length > 0 &&
         (message[length - 1] == '\r' || message[length - 1] == '\n' || message[length - 1] == '.'))
    length--;
  message[length] = '\0';
  krpc_set_error_cause(message);
#elif defined(KRPC_ERROR_MESSAGES)
  krpc_set_error_cause(strerror(errno));
#endif
}

#endif
