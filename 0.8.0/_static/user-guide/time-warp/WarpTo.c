#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Warp to") != KRPC_OK) return 1;
  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  // [docs]
  double ut, time_to_apoapsis;
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_UT(conn, &ut);
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_Orbit_TimeToApoapsis(conn, &time_to_apoapsis, orbit);
  double apoapsis_ut = ut + time_to_apoapsis;
  krpc_SpaceCenter_WarpTo(conn, apoapsis_ut - 10, 100000, 2);
  // [/docs]

  krpc_disconnect(conn);
}
