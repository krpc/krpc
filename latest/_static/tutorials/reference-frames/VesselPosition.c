#include <unistd.h>
#include <math.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Vessel position");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);
  krpc_SpaceCenter_ReferenceFrame_t body_frame;
  krpc_SpaceCenter_CelestialBody_ReferenceFrame(conn, &body_frame, body);

  krpc_tuple_double_double_double_t position;
  krpc_SpaceCenter_Vessel_Position(conn, &position, vessel, body_frame);
  printf("%.2f, %.2f, %.2f\n", position.e0, position.e1, position.e2);
}
