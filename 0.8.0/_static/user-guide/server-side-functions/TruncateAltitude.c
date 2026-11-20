#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Truncate altitude example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  krpc_schema_ProcedureCall mean_altitude;
  krpc_SpaceCenter_Flight_MeanAltitude_call(&mean_altitude, flight);
  // [docs]
  // Truncate the altitude to a whole number of meters
  krpc_KRPC_Expression_t altitude, expr;
  krpc_KRPC_Expression_Call(conn, &altitude, &mean_altitude);
  krpc_free_procedure_call(&mean_altitude);
  krpc_KRPC_Type_t int_type;
  krpc_KRPC_Type_Int(conn, &int_type);
  krpc_KRPC_Expression_Cast(conn, &expr, altitude, int_type);
  // [/docs]

  krpc_disconnect(conn);
}
