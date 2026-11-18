#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <math.h>
#include <stdio.h>
#include <string.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "EditorTorque") != KRPC_OK) return 1;

  krpc_dictionary_string_object_t bodies = KRPC_NULL_DICTIONARY;
  krpc_SpaceCenter_Bodies(conn, &bodies);
  krpc_SpaceCenter_CelestialBody_t body = KRPC_NULL;
  for (size_t i = 0; i < bodies.size; i++)
    if (!strcmp(bodies.entries[i].key, "Kerbin"))
      body = bodies.entries[i].value;
  for (size_t i = 0; i < bodies.size; i++) krpc_free(bodies.entries[i].key);
  KRPC_FREE_DICTIONARY(bodies);
  // [docs]
  double density, gravity;
  krpc_SpaceCenter_CelestialBody_OceanDensity(conn, &density, body);
  krpc_SpaceCenter_CelestialBody_SurfaceGravity(conn, &gravity, body);
  krpc_SpaceCenter_Editor_t editor;
  krpc_SpaceCenter_Editor(conn, &editor);
  krpc_SpaceCenter_EditorVessel_t vessel;
  krpc_SpaceCenter_Editor_Vessel(conn, &vessel, editor);
  krpc_SpaceCenter_ReferenceFrame_t vessel_frame;
  krpc_SpaceCenter_EditorVessel_ReferenceFrame(conn, &vessel_frame, vessel);
  krpc_SpaceCenter_SimulatedFlight_t flight;
  krpc_SpaceCenter_EditorVessel_Flight(conn, &flight, vessel, vessel_frame);
  // The vessel frame's z axis points out of the bottom, so up is its -z
  // axis
  double side = sqrt(0.5);
  krpc_tuple_double_double_double_t zero = {0, 0, 0};
  krpc_tuple_double_double_double_double_t rotation = {0, side, 0, side};
  krpc_SpaceCenter_ReferenceFrame_t up_frame;
  krpc_SpaceCenter_ReferenceFrame_CreateRelative(
      conn, &up_frame, vessel_frame, &zero, &rotation, &zero, &zero);
  krpc_SpaceCenter_SimulatedFlight_set_SurfaceReferenceFrame(conn, flight,
                                                             up_frame);
  krpc_tuple_double_double_double_t torque;
  krpc_SpaceCenter_SimulatedFlight_BuoyantTorqueAt(conn, &torque, flight,
                                                   density, gravity);
  // [/docs]
  printf("Torque = (%.0f, %.0f, %.0f) N m\n", torque.e0, torque.e1,
         torque.e2);

  krpc_disconnect(conn);
}
