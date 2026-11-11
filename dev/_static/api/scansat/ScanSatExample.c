#include <krpc_cnano.h>
#include <krpc_cnano/services/scan_sat.h>
#include <krpc_cnano/services/space_center.h>
#include <stdbool.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "SCANsat Example") != KRPC_OK) return 1;
  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  krpc_list_object_t scanners = KRPC_NULL_LIST;
  krpc_ScanSat_Scanners(conn, &scanners, vessel);
  for (size_t i = 0; i < scanners.size; i++)
    krpc_ScanSat_Scanner_set_Active(conn, scanners.items[i], true);

  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);
  double coverage;
  krpc_ScanSat_Coverage(conn, &coverage, body, KRPC_SCANSAT_SCANTYPE_ALTIMETRY);
  printf("Altimetry coverage: %.1f%%\n", coverage * 100);

  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  double latitude, longitude, elevation;
  bool isNull;
  krpc_SpaceCenter_Flight_Latitude(conn, &latitude, flight);
  krpc_SpaceCenter_Flight_Longitude(conn, &longitude, flight);
  krpc_ScanSat_Elevation(conn, &elevation, &isNull, body, latitude, longitude);
  if (isNull)
    printf("The terrain below the vessel is not scanned\n");
  else
    printf("The terrain below the vessel is at %.0f m\n", elevation);
}
