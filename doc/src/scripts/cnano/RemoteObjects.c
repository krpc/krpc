#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#define sleep(seconds) Sleep((seconds) * 1000)
#else
#include <unistd.h>
#endif
#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open(&conn, "COM0") != KRPC_OK)
    return 1;
  if (krpc_connect(conn, "Remote objects example") != KRPC_OK)
    return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);

  // Each read gets the current altitude from the game
  double altitude;
  krpc_SpaceCenter_Flight_MeanAltitude(conn, &altitude, flight);
  printf("%.2f\n", altitude);
  sleep(1);
  krpc_SpaceCenter_Flight_MeanAltitude(conn, &altitude, flight);
  printf("%.2f\n", altitude);

  // Two objects for the same vessel are equal
  krpc_SpaceCenter_Vessel_t active;
  krpc_SpaceCenter_ActiveVessel(conn, &active);
  printf("%d\n", vessel == active);

  krpc_disconnect(conn);
}
