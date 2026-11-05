#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Altitude in kilometers example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);

  // Build a function that computes
  // the vessel's altitude in kilometers
  krpc_schema_ProcedureCall mean_altitude;
  krpc_SpaceCenter_Flight_MeanAltitude_call(&mean_altitude, flight);
  krpc_KRPC_Expression_t altitude, scale, expr;
  krpc_KRPC_Expression_Call(conn, &altitude, &mean_altitude);
  krpc_free_procedure_call(&mean_altitude);
  krpc_KRPC_Expression_ConstantDouble(conn, &scale, 1000);
  krpc_KRPC_Expression_Divide(conn, &expr, altitude, scale);

  // Run the function in a loop, as this client has no streams
  while (true) {
    krpc_bytes_t data = KRPC_BYTES_NULL;
    if (krpc_run_function(conn, &data, NULL, expr, NULL) != KRPC_OK) break;
    double value;
    pb_istream_t stream = pb_istream_from_buffer(data.data, data.size);
    krpc_decode_double(&stream, &value);
    KRPC_FREE_BYTES(data);
    printf("Altitude: %.3f km\n", value);
  }

  krpc_disconnect(conn);
}
