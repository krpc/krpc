#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Not loaded example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);

  // above = mean_altitude > 1000, or false if the vessel is not loaded
  krpc_KRPC_Type_t bool_type;
  krpc_KRPC_Type_Bool(conn, &bool_type);
  krpc_KRPC_Expression_t above;
  krpc_KRPC_Expression_Variable(conn, &above, "above", bool_type);

  krpc_schema_ProcedureCall mean_altitude;
  krpc_SpaceCenter_Flight_MeanAltitude_call(&mean_altitude, flight);
  krpc_KRPC_Expression_t altitude, limit, comparison, assign_above;
  krpc_KRPC_Expression_Call(conn, &altitude, &mean_altitude);
  krpc_free_procedure_call(&mean_altitude);
  krpc_KRPC_Expression_ConstantDouble(conn, &limit, 1000);
  krpc_KRPC_Expression_GreaterThan(conn, &comparison, altitude, limit);
  krpc_KRPC_Expression_Assign(conn, &assign_above, above, comparison);

  krpc_KRPC_Expression_t no, assign_false, try_catch;
  krpc_KRPC_Expression_ConstantBool(conn, &no, false);
  krpc_KRPC_Expression_Assign(conn, &assign_false, above, no);
  krpc_KRPC_Expression_TryCatch(conn, &try_catch, assign_above, "KRPC",
                                "ObjectNotLoadedException", KRPC_NULL,
                                assign_false);

  krpc_KRPC_Expression_t variable_items[1] = {above};
  krpc_KRPC_Expression_t statement_items[2] = {try_catch, above};
  krpc_list_object_t variables = {1, variable_items};
  krpc_list_object_t statements = {2, statement_items};
  krpc_KRPC_Expression_t above_1000m;
  krpc_KRPC_Expression_BlockWithVariables(conn, &above_1000m, &variables,
                                          &statements);

  // Run the function, as this client has no events
  krpc_bytes_t data = KRPC_BYTES_NULL;
  krpc_run_function(conn, &data, NULL, above_1000m, NULL);
  bool reached;
  pb_istream_t stream = pb_istream_from_buffer(data.data, data.size);
  krpc_decode_bool(&stream, &reached);
  KRPC_FREE_BYTES(data);
  if (reached) printf("Altitude reached 1000m\n");

  krpc_disconnect(conn);
}
