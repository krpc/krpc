#pragma once

#include "krpc_cnano/communication.h"
#include "krpc_cnano/error.h"
#include "krpc_cnano/krpc.pb.h"

/* How much of a message to hold in memory while it is written to or read from the connection.
 * A message larger than this is carried in as many passes as it takes, so this bounds the
 * memory a call costs and never the size of a message it can carry. Two buffers of this size
 * are used, one for a message being sent and one for a message being received, and neither
 * outlives the call it is made for.
 *
 * A message that fits is also cheaper to send, as its size can be written in front of it
 * rather than measured by a pass over it first, so this is worth setting above the largest
 * call a program makes. The default is small on Arduino, where a kilobyte is a large share of
 * the memory there is, and where calls tend to be small ones. */
#ifndef KRPC_BUFFER_SIZE
#ifdef KRPC_COMMUNICATION_ARDUINO
#define KRPC_BUFFER_SIZE 128
#else
#define KRPC_BUFFER_SIZE 1024
#endif
#endif

/* A message is sent with the room its size needs left in front of it, which is up to five
   bytes, so a buffer has to be larger than that to hold any of the message at all. */
#if KRPC_BUFFER_SIZE < 16
#error KRPC_BUFFER_SIZE must be at least 16
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* Connect to a kRPC server using the given communication handle. Waits for the server to
 * accept the connection. Returns KRPC_ERROR_CONNECTION_REFUSED, and closes the connection, if
 * the server denies it or does not support the client's protocol version. Over TCP/IP or a
 * local socket, any other failed connect also closes the connection. Over a serial port or a
 * custom transport, a partial message from an earlier session on the line can garble the
 * connection request or its response. In that case this returns KRPC_ERROR_CONNECTION_FAILED
 * with the connection left open, and the program calls this again to retry. One connection is
 * open at a time. */
krpc_error_t krpc_connect(krpc_connection_t connection, const char* clientName);

/* Tell the server the connection is ending, then close it. On a serial port this is how the
 * server learns the session has ended. */
krpc_error_t krpc_disconnect(krpc_connection_t connection);

/* Make an RPC call. Returns a procedure result containing the return value, if any. Once the
 * session has ended, returns KRPC_ERROR_CONNECTION_FAILED. */
krpc_error_t krpc_invoke(krpc_connection_t connection, krpc_schema_ProcedureResult* result,
                         krpc_schema_ProcedureCall* call);

#ifdef __cplusplus
}  // extern "C"
#endif
