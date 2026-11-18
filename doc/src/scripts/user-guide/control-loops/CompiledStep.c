#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>
#include <krpc_cnano/services/std_lib.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Compiled step example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  krpc_SpaceCenter_Control_t control;
  krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);
  double target_pitch = 45;
  // [docs]
  // control.pitch = clamp(0.05 * (target_pitch - flight.pitch), -1, 1)
  krpc_schema_ProcedureCall pitch_call;
  krpc_SpaceCenter_Flight_Pitch_call(&pitch_call, flight);
  krpc_KRPC_Expression_t target, pitch, error, gain, scaled;
  krpc_KRPC_Expression_ConstantDouble(conn, &target, target_pitch);
  krpc_KRPC_Expression_Call(conn, &pitch, &pitch_call);
  krpc_free_procedure_call(&pitch_call);
  krpc_KRPC_Expression_Subtract(conn, &error, target, pitch);
  krpc_KRPC_Expression_ConstantDouble(conn, &gain, 0.05);
  krpc_KRPC_Expression_Multiply(conn, &scaled, gain, error);

  krpc_KRPC_Expression_t lower, upper, command;
  krpc_KRPC_Expression_ConstantDouble(conn, &lower, -1);
  krpc_KRPC_Expression_ConstantDouble(conn, &upper, 1);
  krpc_schema_ProcedureCall clamp_call;
  krpc_StdLib_Clamp_call_template(&clamp_call);
  krpc_dictionary_entry_int32_object_t clamp_entries[3] = {
      {0, scaled}, {1, lower}, {2, upper}};
  krpc_dictionary_int32_object_t clamp_arguments = {3, clamp_entries};
  krpc_KRPC_Expression_CallWithArguments(conn, &command, &clamp_call,
                                         &clamp_arguments);
  krpc_free_procedure_call(&clamp_call);

  krpc_KRPC_Type_t float_type;
  krpc_KRPC_Type_Float(conn, &float_type);
  krpc_KRPC_Expression_t instance, value, step;
  krpc_KRPC_Expression_ConstantObject(conn, &instance, control);
  krpc_KRPC_Expression_Cast(conn, &value, command, float_type);
  krpc_schema_ProcedureCall setter_call;
  krpc_SpaceCenter_Control_set_Pitch_call_template(&setter_call);
  krpc_dictionary_entry_int32_object_t setter_entries[2] = {{0, instance},
                                                            {1, value}};
  krpc_dictionary_int32_object_t setter_arguments = {2, setter_entries};
  krpc_KRPC_Expression_CallWithArguments(conn, &step, &setter_call,
                                         &setter_arguments);
  krpc_free_procedure_call(&setter_call);
  krpc_compile_function(conn, step);

  while (krpc_run_function(conn, NULL, NULL, step, NULL) == KRPC_OK) {
  }
  // [/docs]

  krpc_disconnect(conn);
}
