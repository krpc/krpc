#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

typedef krpc_error_t (*call_template_t)(krpc_schema_ProcedureCall*);

// A property of the part parameter
static void get(krpc_connection_t conn, krpc_KRPC_Expression_t* result,
                krpc_KRPC_Expression_t part, call_template_t property) {
  krpc_schema_ProcedureCall call;
  property(&call);
  krpc_dictionary_entry_int32_object_t entries[1] = {{0, part}};
  krpc_dictionary_int32_object_t arguments = {1, entries};
  krpc_KRPC_Expression_CallWithArguments(conn, result, &call, &arguments);
  krpc_free_procedure_call(&call);
}

// A function of the part parameter
static void function(krpc_connection_t conn, krpc_KRPC_Expression_t* result,
                     krpc_KRPC_Expression_t part,
                     krpc_KRPC_Expression_t body) {
  krpc_list_object_t parameters = {1, &part};
  krpc_KRPC_Expression_Lambda(conn, result, &parameters, body);
}

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "PressureLimitsFunction") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  // [docs]
  krpc_KRPC_Type_t part_type;
  krpc_KRPC_Type_ClassTypeById(conn, &part_type,
                               KRPC_CLASS_SPACECENTER_PART);
  krpc_KRPC_Expression_t part;
  krpc_KRPC_Expression_Parameter(conn, &part, "part", part_type);

  // The parts over their pressure limit
  krpc_KRPC_Expression_t static_pressure, dynamic_pressure, max_pressure;
  get(conn, &static_pressure, part,
      krpc_SpaceCenter_Part_StaticPressure_call_template);
  get(conn, &dynamic_pressure, part,
      krpc_SpaceCenter_Part_DynamicPressure_call_template);
  get(conn, &max_pressure, part,
      krpc_SpaceCenter_Part_MaxPressure_call_template);
  krpc_KRPC_Expression_t pressure, over, is_over;
  krpc_KRPC_Expression_Add(conn, &pressure, static_pressure,
                           dynamic_pressure);
  krpc_KRPC_Expression_GreaterThan(conn, &over, pressure, max_pressure);
  function(conn, &is_over, part, over);
  krpc_schema_ProcedureCall all_call;
  krpc_SpaceCenter_Parts_All_call(&all_call, parts);
  krpc_KRPC_Expression_t all_parts, over_parts;
  krpc_KRPC_Expression_Call(conn, &all_parts, &all_call);
  krpc_free_procedure_call(&all_call);
  krpc_KRPC_Expression_Where(conn, &over_parts, all_parts, is_over);

  // Their titles
  krpc_KRPC_Expression_t title, title_of, titles, expr;
  get(conn, &title, part, krpc_SpaceCenter_Part_Title_call_template);
  function(conn, &title_of, part, title);
  krpc_KRPC_Expression_Select(conn, &titles, over_parts, title_of);
  krpc_KRPC_Expression_ToList(conn, &expr, titles);

  krpc_bytes_t data = KRPC_BYTES_NULL;
  krpc_run_function(conn, &data, NULL, expr, NULL);
  krpc_list_string_t over_limit = KRPC_NULL_LIST;
  pb_istream_t stream = pb_istream_from_buffer(data.data, data.size);
  krpc_decode_list_string(&stream, &over_limit);
  KRPC_FREE_BYTES(data);
  // [/docs]
  for (size_t i = 0; i < over_limit.size; i++) {
    printf("%s\n", over_limit.items[i]);
    krpc_free(over_limit.items[i]);
  }
  KRPC_FREE_LIST(over_limit);

  krpc_disconnect(conn);
}
