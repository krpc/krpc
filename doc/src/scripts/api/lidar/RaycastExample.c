#include <math.h>
#include <stdio.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

static double pi = 3.1415926535897;

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK)
    return 1;
  if (krpc_connect(conn, "Raycast Example") != KRPC_OK)
    return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  krpc_SpaceCenter_Part_t part;
  krpc_SpaceCenter_Parts_Root(conn, &part, parts);
  krpc_SpaceCenter_ReferenceFrame_t frame;
  krpc_SpaceCenter_Part_ReferenceFrame(conn, &frame, part);

  // A fan of 8 rays below the part, 10 degrees apart
  krpc_tuple_double_double_double_t origin = { 0, 0, 0 };
  for (int i = 0; i < 8; i++) {
    double angle = (10 * i - 35) * pi / 180;
    krpc_tuple_double_double_double_t direction = { sin(angle), -cos(angle), 0 };
    double distance;
    krpc_SpaceCenter_RaycastDistance(conn, &distance, &origin, &direction, frame);
    printf("%.2f\n", distance);
  }
}
