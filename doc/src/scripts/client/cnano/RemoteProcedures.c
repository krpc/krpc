#include <krpc.h>
#include <krpc/services/space_center.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Remote Procedures example");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  krpc_SpaceCenter_Vessel_set_Name(conn, vessel, "My Vessel");

  // Get a handle to a Flight object for the vessel
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  // Get the altiude
  double altitude;
  krpc_SpaceCenter_Flight_MeanAltitude(conn, &altitude, flight);
  printf("%.2f\n", altitude);
}
