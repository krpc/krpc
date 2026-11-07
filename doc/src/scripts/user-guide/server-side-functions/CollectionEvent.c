#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Out of fuel example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  krpc_list_object_t vessel_engines = KRPC_NULL_LIST;
  krpc_SpaceCenter_Parts_Engines(conn, &vessel_engines, parts);

  // A function taking an engine, returning true if it is out of fuel
  krpc_KRPC_Type_t engine_type;
  krpc_KRPC_Type_ClassType(conn, &engine_type, "SpaceCenter", "Engine");
  krpc_KRPC_Expression_t engine;
  krpc_KRPC_Expression_Parameter(conn, &engine, "engine", engine_type);

  krpc_schema_ProcedureCall has_fuel;
  krpc_SpaceCenter_Engine_HasFuel_call(&has_fuel, vessel_engines.items[0]);
  krpc_dictionary_entry_int32_object_t entries[1] = {{0, engine}};
  krpc_dictionary_int32_object_t arguments = {1, entries};
  krpc_KRPC_Expression_t engine_has_fuel, no_fuel, out_of_fuel;
  krpc_KRPC_Expression_CallWithArguments(conn, &engine_has_fuel, &has_fuel,
                                         &arguments);
  krpc_free_procedure_call(&has_fuel);
  krpc_KRPC_Expression_Not(conn, &no_fuel, engine_has_fuel);
  krpc_list_object_t parameters = {1, &engine};
  krpc_KRPC_Expression_Lambda(conn, &out_of_fuel, &parameters, no_fuel);

  // Whether any engine satisfies it
  krpc_schema_ProcedureCall engines_call;
  krpc_SpaceCenter_Parts_Engines_call(&engines_call, parts);
  krpc_KRPC_Expression_t engines, expr;
  krpc_KRPC_Expression_Call(conn, &engines, &engines_call);
  krpc_free_procedure_call(&engines_call);
  krpc_KRPC_Expression_Any(conn, &expr, engines, out_of_fuel);

  // Run the function, as this client has no events
  krpc_bytes_t data = KRPC_BYTES_NULL;
  krpc_run_function(conn, &data, NULL, expr, NULL);
  bool any_out_of_fuel;
  pb_istream_t stream = pb_istream_from_buffer(data.data, data.size);
  krpc_decode_bool(&stream, &any_out_of_fuel);
  KRPC_FREE_BYTES(data);
  if (any_out_of_fuel) printf("An engine has run out of fuel\n");

  KRPC_FREE_LIST(vessel_engines);
  krpc_disconnect(conn);
}
