#pragma once

#include <krpc/error.h>

#include <stddef.h>
#include <stdint.h>

#if !defined(KRPC_COMMS_CUSTOM)
  #if defined(ARDUINO)
    #define KRPC_COMMUNICATION_ARDUINO
    #ifndef __cplusplus
    #error "Require a C++ compiler to build kRPC for Arduino"
    #endif
    #include <HardwareSerial.h>
  #elif !defined(KRPC_COMMS_CUSTOM)
    #define KRPC_COMMUNICATION_POSIX
  #endif
#endif

#ifdef __cplusplus
extern "C" {
#endif

#ifdef KRPC_COMMUNICATION_POSIX
typedef int krpc_connection_t;
#endif

#ifdef KRPC_COMMUNICATION_ARDUINO
typedef HardwareSerial * krpc_connection_t;
#endif

/* Open a connection */
krpc_error_t krpc_open(krpc_connection_t * connection, const void * arg);
/* Close a connection */
krpc_error_t krpc_close(krpc_connection_t connection);
/* Read count bytes of data from the connection into buf */
krpc_error_t krpc_read(krpc_connection_t connection, uint8_t * buf, size_t count);
/* Write count bytes of data from into buf to the connection */
krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t * buf, size_t count);

#ifdef __cplusplus
}  // extern "C"
#endif
