#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <krpc_cnano/services/trajectories.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Trajectories Example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);
  krpc_SpaceCenter_ReferenceFrame_t frame;
  krpc_SpaceCenter_CelestialBody_ReferenceFrame(conn, &frame, body);

  krpc_tuple_double_double_double_t position;
  bool no_impact;
  krpc_Trajectories_ImpactPosition(conn, &position, &no_impact, frame);
  if (no_impact)
    printf("No impact\n");
  else
    printf("%.0f, %.0f, %.0f\n", position.e0, position.e1, position.e2);
  krpc_disconnect(conn);
}
