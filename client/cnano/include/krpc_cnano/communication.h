#pragma once

#include <krpc_cnano/error.h>
#include <stddef.h>
#include <stdint.h>

#if !defined(KRPC_COMMUNICATION_CUSTOM)
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
