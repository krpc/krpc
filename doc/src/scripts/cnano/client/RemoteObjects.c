#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Remote objects example");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);

  // Each read gets the current altitude from the game
  double altitude;
  krpc_SpaceCenter_Flight_MeanAltitude(conn, &altitude, flight);
  printf("%.2f\n", altitude);
  krpc_SpaceCenter_Flight_MeanAltitude(conn, &altitude, flight);
  printf("%.2f\n", altitude);

  // Two objects for the same vessel are equal
  krpc_SpaceCenter_Vessel_t active;
  krpc_SpaceCenter_ActiveVessel(conn, &active);
  printf("%d\n", vessel == active);

  krpc_disconnect(conn);
}
