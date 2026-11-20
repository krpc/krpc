#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "DeployParachutesFunction") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  // [docs]
  // for each parachute in the vessel's parts: deploy it
  krpc_KRPC_Type_t parachute_type;
  krpc_KRPC_Type_ClassTypeById(conn, &parachute_type,
                               KRPC_CLASS_SPACECENTER_PARACHUTE);
  krpc_KRPC_Expression_t parachute;
  krpc_KRPC_Expression_Variable(conn, &parachute, "parachute",
                                parachute_type);

  krpc_schema_ProcedureCall deploy_call;
  krpc_SpaceCenter_Parachute_Deploy_call_template(&deploy_call);
  krpc_dictionary_entry_int32_object_t entries[1] = {{0, parachute}};
  krpc_dictionary_int32_object_t arguments = {1, entries};
  krpc_KRPC_Expression_t deploy;
  krpc_KRPC_Expression_CallWithArguments(conn, &deploy, &deploy_call,
                                         &arguments);
  krpc_free_procedure_call(&deploy_call);

  krpc_schema_ProcedureCall parachutes_call;
  krpc_SpaceCenter_Parts_Parachutes_call(&parachutes_call, parts);
  krpc_KRPC_Expression_t parachutes, loop;
  krpc_KRPC_Expression_Call(conn, &parachutes, &parachutes_call);
  krpc_free_procedure_call(&parachutes_call);
  krpc_KRPC_Expression_ForEach(conn, &loop, parachute, parachutes, deploy);

  krpc_list_object_t variables = {1, &parachute};
  krpc_list_object_t statements = {1, &loop};
  krpc_KRPC_Expression_t program;
  krpc_KRPC_Expression_BlockWithVariables(conn, &program, &variables,
                                          &statements);

  krpc_run_function(conn, NULL, NULL, program, NULL);
  // [/docs]

  krpc_disconnect(conn);
}
