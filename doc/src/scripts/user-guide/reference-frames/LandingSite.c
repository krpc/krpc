#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#define sleep(seconds) Sleep((seconds) * 1000)
#else
#include <unistd.h>
#endif
#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <krpc_cnano/services/ui.h>
#include <krpc_cnano/services/drawing.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open(&conn, "COM0") != KRPC_OK)
    return 1;
  if (krpc_connect(conn, "Landing site") != KRPC_OK)
    return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);

  // Define the landing site as the top of the VAB
  double landing_latitude = -(0.0+(5.0/60.0)+(48.38/60.0/60.0));
  double landing_longitude = -(74.0+(37.0/60.0)+(12.2/60.0/60.0));
  double landing_altitude = 111;

  // Determine landing site reference frame
  // (orientation: x=zenith, y=north, z=east)
  krpc_SpaceCenter_ReferenceFrame_t landing_reference_frame;
  krpc_SpaceCenter_CelestialBody_SurfaceReferenceFrame(
    conn, &landing_reference_frame, body,
    landing_latitude, landing_longitude, landing_altitude);

  // Draw axes
  krpc_Drawing_AddAxes(conn, NULL, landing_reference_frame, 10, true);

  while (true)
    sleep(1);
}
