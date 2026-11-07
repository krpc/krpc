#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Throw exception example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Resources_t resources;
  krpc_SpaceCenter_Vessel_Resources(conn, &resources, vessel);
  // [docs]
  krpc_schema_ProcedureCall amount;
  krpc_SpaceCenter_Resources_Amount_call(&amount, resources, "LiquidFuel");
  krpc_KRPC_Expression_t fuel, limit, low, message, thrown, expr;
  krpc_KRPC_Expression_Call(conn, &fuel, &amount);
  krpc_free_procedure_call(&amount);
  krpc_KRPC_Expression_ConstantDouble(conn, &limit, 100);
  krpc_KRPC_Expression_LessThan(conn, &low, fuel, limit);
  krpc_KRPC_Expression_ConstantString(conn, &message, "not enough fuel");
  krpc_KRPC_Expression_Throw(conn, &thrown, "KRPC", "InvalidOperationException",
                             message);
  krpc_KRPC_Expression_IfThen(conn, &expr, low, thrown);
  krpc_run_function(conn, NULL, NULL, expr, NULL);
  // [/docs]

  krpc_disconnect(conn);
}
