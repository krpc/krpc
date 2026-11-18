#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

// [helpers]
typedef krpc_error_t (*call_template_t)(krpc_schema_ProcedureCall*);

// A property of the engine parameter
static void get(krpc_connection_t conn, krpc_KRPC_Expression_t* result,
                krpc_KRPC_Expression_t engine, call_template_t property) {
  krpc_schema_ProcedureCall call;
  property(&call);
  krpc_dictionary_entry_int32_object_t entries[1] = {{0, engine}};
  krpc_dictionary_int32_object_t arguments = {1, entries};
  krpc_KRPC_Expression_CallWithArguments(conn, result, &call, &arguments);
  krpc_free_procedure_call(&call);
}

// A function of the engine parameter
static void function(krpc_connection_t conn, krpc_KRPC_Expression_t* result,
                     krpc_KRPC_Expression_t engine,
                     krpc_KRPC_Expression_t body) {
  krpc_list_object_t parameters = {1, &engine};
  krpc_KRPC_Expression_Lambda(conn, result, &parameters, body);
}
// [/helpers]

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Combined Isp function") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  // [docs]
  krpc_KRPC_Type_t engine_type;
  krpc_KRPC_Type_ClassTypeById(conn, &engine_type,
                               KRPC_CLASS_SPACECENTER_ENGINE);
  krpc_KRPC_Expression_t engine;
  krpc_KRPC_Expression_Parameter(conn, &engine, "engine", engine_type);

  // The active engines with fuel
  krpc_KRPC_Expression_t active, has_fuel, fueled, is_fueled;
  get(conn, &active, engine, krpc_SpaceCenter_Engine_Active_call_template);
  get(conn, &has_fuel, engine,
      krpc_SpaceCenter_Engine_HasFuel_call_template);
  krpc_KRPC_Expression_ConditionalAnd(conn, &fueled, active, has_fuel);
  function(conn, &is_fueled, engine, fueled);
  krpc_schema_ProcedureCall engines_call;
  krpc_SpaceCenter_Parts_Engines_call(&engines_call, parts);
  krpc_KRPC_Expression_t all_engines, engines;
  krpc_KRPC_Expression_Call(conn, &all_engines, &engines_call);
  krpc_free_procedure_call(&engines_call);
  krpc_KRPC_Expression_Where(conn, &engines, all_engines, is_fueled);

  // Their total thrust, divided by their total fuel consumption
  krpc_KRPC_Expression_t thrust, isp, consumption;
  get(conn, &thrust, engine,
      krpc_SpaceCenter_Engine_AvailableThrust_call_template);
  get(conn, &isp, engine,
      krpc_SpaceCenter_Engine_SpecificImpulse_call_template);
  krpc_KRPC_Expression_Divide(conn, &consumption, thrust, isp);
  krpc_KRPC_Expression_t thrust_of, consumption_of;
  function(conn, &thrust_of, engine, thrust);
  function(conn, &consumption_of, engine, consumption);
  krpc_KRPC_Expression_t thrusts, consumptions;
  krpc_KRPC_Expression_Select(conn, &thrusts, engines, thrust_of);
  krpc_KRPC_Expression_Select(conn, &consumptions, engines, consumption_of);
  krpc_KRPC_Expression_t total_thrust, fuel_consumption, expr;
  krpc_KRPC_Expression_Sum(conn, &total_thrust, thrusts);
  krpc_KRPC_Expression_Sum(conn, &fuel_consumption, consumptions);
  krpc_KRPC_Expression_Divide(conn, &expr, total_thrust, fuel_consumption);

  krpc_bytes_t data = KRPC_BYTES_NULL;
  krpc_run_function(conn, &data, NULL, expr, NULL);
  float combined_isp;
  pb_istream_t stream = pb_istream_from_buffer(data.data, data.size);
  krpc_decode_float(&stream, &combined_isp);
  KRPC_FREE_BYTES(data);
  // [/docs]
  printf("Combined Isp = %.0f seconds\n", combined_isp);

  krpc_disconnect(conn);
}
