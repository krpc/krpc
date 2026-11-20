#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Warp factor") != KRPC_OK) return 1;

  // [docs]
  int32_t maximum;
  krpc_SpaceCenter_MaximumRailsWarpFactor(conn, &maximum);
  printf("%d\n", (int)maximum);
  krpc_SpaceCenter_set_RailsWarpFactor(conn, 3);
  // [/docs]

  krpc_disconnect(conn);
}
