#pragma once

#include <krpc_cnano/error.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

/* The platforms with built-in transports. On any other platform a program supplies its own. */
#if defined(ARDUINO)
#define KRPC_PLATFORM_ARDUINO
#ifndef __cplusplus
#error "Require a C++ compiler to build kRPC for Arduino"
#endif
#include <Arduino.h>
#elif defined(_WIN32)
#define KRPC_PLATFORM_WINDOWS
#elif defined(__unix__) || defined(__APPLE__)
#define KRPC_PLATFORM_POSIX
#endif

/* Whether the local socket transport is built. Windows declares the socket address in afunix.h,
   which older SDKs and MinGW releases lack. */
#if defined(KRPC_PLATFORM_POSIX)
#define KRPC_HAS_LOCALSOCKET
#elif defined(KRPC_PLATFORM_WINDOWS)
#if defined(__has_include)
#if __has_include(<afunix.h>)
#define KRPC_HAS_LOCALSOCKET
#endif
#else
#define KRPC_HAS_LOCALSOCKET
#endif
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* A transport, as the functions that carry bytes over it. Each takes the handle of the
   connection it was opened with. */
typedef struct {
  /* Read exactly count bytes. Returns KRPC_ERROR_EOF once the peer has gone, and KRPC_ERROR_IO
     when the read fails. Before either, krpc_set_error_cause gives the failure's message. */
  krpc_error_t (*read)(uintptr_t handle, uint8_t* buf, size_t count);
  /* Write all count bytes */
  krpc_error_t (*write)(uintptr_t handle, const uint8_t* buf, size_t count);
  /* Close the connection, or NULL for a transport whose connections need no closing */
  krpc_error_t (*close)(uintptr_t handle);
  /* Whether the link outlives the client, as a serial line does. A client that resets can find
     messages from its earlier session on such a link, and skips them while it connects. */
  bool resumable;
} krpc_transport_t;

/* A connection: the transport it goes over, and the handle the transport reads and writes */
typedef struct {
  const krpc_transport_t* transport;
  uintptr_t handle;
} krpc_connection_t;

/* Each krpc_open_* function writes the connection it opens. A failed open returns
   KRPC_ERROR_CONNECTION, or KRPC_ERROR_ARGUMENT for an argument it rejects, and writes a
   connection with no transport, which every call reports as an error. */

/* Read count bytes of data from the connection into buf */
krpc_error_t krpc_read(krpc_connection_t connection, uint8_t* buf, size_t count);
/* Write count bytes of data from buf to the connection */
krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t* buf, size_t count);
/* Close a connection */
krpc_error_t krpc_close(krpc_connection_t connection);

#if defined(KRPC_PLATFORM_POSIX) || defined(KRPC_PLATFORM_WINDOWS)

/* Give the system's message for the last error, errno or GetLastError, as the cause */
void krpc_set_system_error_cause(void);

/* A serial port, whose handle is a file descriptor, or a Windows HANDLE */
extern const krpc_transport_t krpc_transport_serial;
/* A connected socket, whose handle is a file descriptor, or a Windows SOCKET */
extern const krpc_transport_t krpc_transport_socket;

/* Open the serial port with the given name, such as "/dev/ttyS0" or "COM3" */
krpc_error_t krpc_open_serial(krpc_connection_t* connection, const char* port);
/* Open a TCP/IP connection to a server at the given address and port */
krpc_error_t krpc_open_tcp(krpc_connection_t* connection, const char* address, uint16_t port);
#ifdef KRPC_HAS_LOCALSOCKET
/* Open a connection to a server on the same machine, over the unix domain socket at path */
krpc_error_t krpc_open_localsocket(krpc_connection_t* connection, const char* path);
#else
/* A call fails to compile, as the library is built without the local socket transport. MSVC has
   no such attribute, so there the call warns from /W3 up and then fails to link. */
#if defined(__clang__)
#define KRPC_UNAVAILABLE(message) __attribute__((unavailable(message)))
#elif defined(__GNUC__)
#define KRPC_UNAVAILABLE(message) __attribute__((error(message)))
#elif defined(_MSC_VER)
#define KRPC_UNAVAILABLE(message) __declspec(deprecated(message))
#else
#define KRPC_UNAVAILABLE(message)
#endif
KRPC_UNAVAILABLE("krpc_open_localsocket needs afunix.h, from Windows SDK 10.0.17134 or later")
krpc_error_t krpc_open_localsocket(krpc_connection_t* connection, const char* path);
#endif

#endif

#ifdef KRPC_PLATFORM_ARDUINO

/* An Arduino Stream, whose handle is a Stream pointer */
extern const krpc_transport_t krpc_transport_stream;

/* Open a connection over a stream the sketch has started, such as a serial port after begin */
krpc_error_t krpc_open_stream(krpc_connection_t* connection, Stream* stream);

#endif

#ifdef __cplusplus
}  // extern "C"
#endif

#ifdef KRPC_PLATFORM_ARDUINO

/* Start a serial port with 8N1 framing, wait for it to be ready, and open a connection over it.
   A template, as the serial classes of the Arduino cores share no base class with begin. */
template <typename Port>
krpc_error_t krpc_open_serial(krpc_connection_t* connection, Port* port, uint32_t speed) {
  port->begin(speed);
  // A USB port is ready once the host has opened it
  while (!*port) {
  }
  return krpc_open_stream(connection, port);
}

#endif
