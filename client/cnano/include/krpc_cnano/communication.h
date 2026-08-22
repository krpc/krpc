#pragma once

#include <krpc_cnano/error.h>
#include <stddef.h>
#include <stdint.h>

#if !defined(KRPC_COMMUNICATION_CUSTOM) && !defined(KRPC_COMMUNICATION_TCP) && \
    !defined(KRPC_COMMUNICATION_LOCALSOCKET)
#if defined(ARDUINO)
#define KRPC_COMMUNICATION_ARDUINO
#ifndef __cplusplus
#error "Require a C++ compiler to build kRPC for Arduino"
#endif
#include <HardwareSerial.h>
#elif defined(_WIN32)
#define KRPC_COMMUNICATION_WINDOWS
#else
#define KRPC_COMMUNICATION_POSIX
#endif
#endif

/* A serial link carries the RPC and stream connections over the one link, so each message sent
   over it is wrapped in a multiplexed message saying which connection it belongs to. A transport
   that opens a connection of its own per server sends the messages themselves.

   A custom transport is taken to carry both connections over one link, as a serial port does;
   one that opens a connection per server is built with KRPC_SINGLE_CONNECTION. */
#if defined(KRPC_COMMUNICATION_POSIX) || defined(KRPC_COMMUNICATION_WINDOWS) || \
    defined(KRPC_COMMUNICATION_ARDUINO) ||                                      \
    (defined(KRPC_COMMUNICATION_CUSTOM) && !defined(KRPC_SINGLE_CONNECTION))
#define KRPC_MULTIPLEXED
#endif

#ifdef __cplusplus
extern "C" {
#endif

#ifdef KRPC_COMMUNICATION_POSIX
typedef int krpc_connection_t;
typedef char krpc_connection_config_t;
#endif

/* A unix domain socket, for a server on the same machine. The connection is a socket, held as
   an integer wide enough for both a POSIX file descriptor and a Windows SOCKET so that the
   winsock headers stay out of this one, and the configuration is the path of the socket, named
   as a serial port is. Like any other socket it opens a connection of its own per server, so it
   is not multiplexed. */
#ifdef KRPC_COMMUNICATION_LOCALSOCKET
typedef uintptr_t krpc_connection_t;
typedef char krpc_connection_config_t;
#endif

#ifdef KRPC_COMMUNICATION_WINDOWS
typedef void* krpc_connection_t;
typedef char krpc_connection_config_t;
#endif

#ifdef KRPC_COMMUNICATION_ARDUINO
typedef HardwareSerial* krpc_connection_t;

typedef struct {
  uint32_t speed;
  uint8_t config;
} krpc_connection_config_t;
#endif

/* A TCP/IP connection to a server reached over a network. The connection is a socket, held as an
   integer wide enough for both a POSIX file descriptor and a Windows SOCKET so that the winsock
   headers stay out of this one. An endpoint is a host and a port rather than a name, so this is
   the one transport whose configuration is a structure on every platform. */
#ifdef KRPC_COMMUNICATION_TCP
typedef uintptr_t krpc_connection_t;

typedef struct {
  const char* address;
  uint16_t port;
} krpc_connection_config_t;
#endif

/* Open a connection */
krpc_error_t krpc_open(krpc_connection_t* connection, const krpc_connection_config_t* config);
/* Close a connection */
krpc_error_t krpc_close(krpc_connection_t connection);
/* Read count bytes of data from the connection into buf */
krpc_error_t krpc_read(krpc_connection_t connection, uint8_t* buf, size_t count);
/* Write count bytes of data from into buf to the connection */
krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t* buf, size_t count);

#ifdef __cplusplus
}  // extern "C"
#endif
