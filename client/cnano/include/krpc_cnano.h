#pragma once

#include "krpc_cnano/communication.h"
#include "krpc_cnano/error.h"
#include "krpc_cnano/krpc.pb.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Connect to a kRPC server using the given communication handle */
krpc_error_t krpc_connect(krpc_connection_t connection, const char * clientName);

/* Make an RPC call. Returns a procedure result containing the return value, if any. */
krpc_error_t krpc_invoke(
  krpc_connection_t connection, krpc_schema_ProcedureResult * result,
  krpc_schema_ProcedureCall * call);

#ifdef __cplusplus
}  // extern "C"
#endif
