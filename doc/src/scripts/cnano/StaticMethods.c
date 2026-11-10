#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Static methods example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_ReferenceFrame_t frame;
  krpc_SpaceCenter_Vessel_ReferenceFrame(conn, &frame, vessel);

  // Call a static method of the Orbit class
  krpc_tuple_double_double_double_t direction;
  krpc_SpaceCenter_Orbit_ReferencePlaneDirection(conn, &direction, frame);
  printf("%.2f, %.2f, %.2f\n", direction.e0, direction.e1, direction.e2);

  krpc_disconnect(conn);
}
