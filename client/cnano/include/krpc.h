#pragma once

#include "krpc/communication.h"
#include "krpc/error.h"
#include "krpc/krpc.pb.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Connect to a kRPC server using the given communication handle */
krpc_error_t krpc_connect(krpc_connection_t connection, const char * clientName);

/* Make an RPC call.
 *
 * Parameters:
 *  fd     - the file descriptor for the connection.
 *  result - message where the result will be stored
 *  call   - message describing the call
 *
 * Returns a procedure result containing the return value, if any.
 */
krpc_error_t krpc_invoke(
  krpc_connection_t connection, krpc_schema_ProcedureResult * result,
  krpc_schema_ProcedureCall * call);

#ifdef __cplusplus
}  // extern "C"
#endif
