#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Structure field example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Comms_t comms;
  krpc_SpaceCenter_Vessel_Comms(conn, &comms, vessel);
  krpc_list_object_t control_path = KRPC_NULL_LIST;
  krpc_SpaceCenter_Comms_ControlPath(conn, &control_path, comms);
  krpc_schema_ProcedureCall site_info;
  krpc_SpaceCenter_CommLink_Start_call(&site_info, control_path.items[0]);
  // [docs]
  // The Name field of a structure a call returns
  krpc_KRPC_Expression_t value, expr;
  krpc_KRPC_Expression_Call(conn, &value, &site_info);
  krpc_free_procedure_call(&site_info);
  krpc_KRPC_Expression_GetFieldByIndex(conn, &expr, value,
                                       KRPC_FIELD_SPACECENTER_COMMNODE_NAME);
  // [/docs]

  KRPC_FREE_LIST(control_path);
  krpc_disconnect(conn);
}
