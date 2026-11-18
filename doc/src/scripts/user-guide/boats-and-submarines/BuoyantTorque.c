#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "BuoyantTorque") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);
  krpc_SpaceCenter_ReferenceFrame_t frame;
  krpc_SpaceCenter_Vessel_SurfaceReferenceFrame(conn, &frame, vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, frame);
  // [docs]
  double density, gravity;
  krpc_SpaceCenter_CelestialBody_OceanDensity(conn, &density, body);
  krpc_SpaceCenter_CelestialBody_SurfaceGravity(conn, &gravity, body);
  krpc_tuple_double_double_double_t torque;
  krpc_SpaceCenter_Flight_BuoyantTorqueAt(conn, &torque, flight, density,
                                          gravity);
  // [/docs]
  printf("Torque = (%.0f, %.0f, %.0f) N m\n", torque.e0, torque.e1,
         torque.e2);

  krpc_disconnect(conn);
}
