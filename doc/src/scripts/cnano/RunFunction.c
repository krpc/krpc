#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Server side function example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);

  // Fill in the remote procedure calls as messages, so they can be passed to
  // the server
  krpc_schema_ProcedureCall calls[3];
  krpc_SpaceCenter_Flight_MeanAltitude_call(&calls[0], flight);
  krpc_SpaceCenter_Flight_Speed_call(&calls[1], flight);
  krpc_SpaceCenter_Flight_VerticalSpeed_call(&calls[2], flight);

  // Build a function that reads the three values
  krpc_KRPC_Expression_t items[3];
  for (int i = 0; i < 3; i++) {
    krpc_KRPC_Expression_Call(conn, &items[i], &calls[i]);
    krpc_free_procedure_call(&calls[i]);
  }
  krpc_list_object_t elements = {3, items};
  krpc_KRPC_Expression_t function;
  krpc_KRPC_Expression_CreateTuple(conn, &function, &elements);

  // Run the function once, so the values come from the same physics tick
  krpc_bytes_t data = KRPC_BYTES_NULL;
  bool is_null;
  krpc_run_function(conn, &data, &is_null, function, NULL);

  // Decode the value at the function's type
  krpc_tuple_double_double_double_t value;
  pb_istream_t stream = pb_istream_from_buffer(data.data, data.size);
  krpc_decode_tuple_double_double_double(&stream, &value);
  KRPC_FREE_BYTES(data);
  printf("Altitude: %.1f m, speed: %.1f m/s, vertical speed: %.1f m/s\n",
         value.e0, value.e1, value.e2);

  krpc_disconnect(conn);
}
