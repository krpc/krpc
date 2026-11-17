#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <krpc_cnano/services/std_lib.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Angle to prograde example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);
  krpc_SpaceCenter_ReferenceFrame_t frame;
  krpc_SpaceCenter_CelestialBody_NonRotatingReferenceFrame(conn, &frame,
                                                           body);

  // [docs]
  krpc_tuple_double_double_double_t direction, prograde;
  krpc_SpaceCenter_Vessel_Direction(conn, &direction, vessel, frame);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, frame);
  krpc_SpaceCenter_Flight_Prograde(conn, &prograde, flight);
  double angle, degrees;
  krpc_StdLib_VectorAngle(conn, &angle, &direction, &prograde);
  krpc_StdLib_RadiansToDegrees(conn, &degrees, angle);
  printf("%f\n", degrees);
  // [/docs]

  krpc_disconnect(conn);
}
