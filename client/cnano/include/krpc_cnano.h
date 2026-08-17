#pragma once

#include "krpc_cnano/communication.h"
#include "krpc_cnano/error.h"
#include "krpc_cnano/krpc.pb.h"

/* How much of a message to hold in memory while it is written to or read from the connection.
 * A message larger than this is carried in as many passes as it takes, so this bounds the
 * memory a call costs and never the size of a message it can carry. Two buffers of this size
 * are used, one for a message being sent and one for a message being received, and neither
 * outlives the call it is made for. */
#ifndef KRPC_BUFFER_SIZE
#define KRPC_BUFFER_SIZE 256
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* Connect to a kRPC server using the given communication handle */
krpc_error_t krpc_connect(krpc_connection_t connection, const char* clientName);

/* Make an RPC call. Returns a procedure result containing the return value, if any. */
krpc_error_t krpc_invoke(krpc_connection_t connection, krpc_schema_ProcedureResult* result,
                         krpc_schema_ProcedureCall* call);

#ifdef __cplusplus
}  // extern "C"
#endif
