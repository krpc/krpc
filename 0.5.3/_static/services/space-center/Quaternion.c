#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "");
  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  krpc_tuple_double_double_double_double_t q;
  krpc_SpaceCenter_Flight_Rotation(conn, &q, flight);
  printf("%.2f %.2f %.2f %.2f\n", q.e0, q.e1, q.e2, q.e3);
}
