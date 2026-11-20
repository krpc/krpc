#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Target example") != KRPC_OK) return 1;

  krpc_list_object_t vessels = KRPC_NULL_LIST;
  krpc_SpaceCenter_Vessels(conn, &vessels);
  krpc_SpaceCenter_Vessel_t some_vessel = vessels.items[0];
  // [docs]
  krpc_schema_ProcedureCall target;
  krpc_SpaceCenter_TargetVessel_call(&target);
  krpc_KRPC_Expression_t current, wanted, expr;
  krpc_KRPC_Expression_Call(conn, &current, &target);
  krpc_free_procedure_call(&target);
  krpc_KRPC_Expression_ConstantObject(conn, &wanted, some_vessel);
  krpc_KRPC_Expression_Equal(conn, &expr, current, wanted);

  // Run the function, as this client has no events
  krpc_bytes_t data = KRPC_BYTES_NULL;
  krpc_run_function(conn, &data, NULL, expr, NULL);
  bool is_target;
  pb_istream_t stream = pb_istream_from_buffer(data.data, data.size);
  krpc_decode_bool(&stream, &is_target);
  KRPC_FREE_BYTES(data);
  // [/docs]

  KRPC_FREE_LIST(vessels);
  krpc_disconnect(conn);
}
