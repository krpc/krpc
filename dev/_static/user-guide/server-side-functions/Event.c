#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Altitude example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);

  // Fill in the remote procedure call as a message,
  // so it can be passed to the server
  krpc_schema_ProcedureCall mean_altitude;
  krpc_SpaceCenter_Flight_MeanAltitude_call(&mean_altitude, flight);

  // Build the function on the server
  krpc_KRPC_Expression_t altitude, limit, expr;
  krpc_KRPC_Expression_Call(conn, &altitude, &mean_altitude);
  krpc_free_procedure_call(&mean_altitude);
  krpc_KRPC_Expression_ConstantDouble(conn, &limit, 1000);
  krpc_KRPC_Expression_GreaterThan(conn, &expr, altitude, limit);

  // Run the function, as this client has no events
  krpc_bytes_t data = KRPC_BYTES_NULL;
  krpc_run_function(conn, &data, NULL, expr, NULL);
  bool reached;
  pb_istream_t stream = pb_istream_from_buffer(data.data, data.size);
  krpc_decode_bool(&stream, &reached);
  KRPC_FREE_BYTES(data);
  if (reached) printf("Altitude reached 1000m\n");

  krpc_disconnect(conn);
}
