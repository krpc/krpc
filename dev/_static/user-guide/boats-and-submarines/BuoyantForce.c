#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "BuoyantForce") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  // [docs]
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  double displacement;
  krpc_SpaceCenter_Flight_Displacement(conn, &displacement, flight);
  double density, gravity, buoyancy;
  krpc_SpaceCenter_CelestialBody_OceanDensity(conn, &density, body);
  krpc_SpaceCenter_CelestialBody_SurfaceGravity(conn, &gravity, body);
  krpc_SpaceCenter_Flight_BuoyantForceAt(conn, &buoyancy, flight, density,
                                         gravity);
  // [/docs]
  printf("Displacement = %.1f m^3, buoyancy = %.0f N\n", displacement,
         buoyancy);

  krpc_disconnect(conn);
}
