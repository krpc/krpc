#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

// A C enumeration is an int, so combining its values is signed bitwise logic
// NOLINTBEGIN(bugprone-signed-bitwise)
int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Flags example") != KRPC_OK) return 1;

  // [docs]
  // Show only probes and ships in map mode
  krpc_SpaceCenter_set_MapFilter(conn,
                                 KRPC_SPACECENTER_MAPFILTERTYPE_PROBES |
                                     KRPC_SPACECENTER_MAPFILTERTYPE_SHIPS);

  // Check whether map mode shows debris
  krpc_SpaceCenter_MapFilterType_t filter;
  krpc_SpaceCenter_MapFilter(conn, &filter);
  printf("%d\n", (filter & KRPC_SPACECENTER_MAPFILTERTYPE_DEBRIS) != 0);

  // Show debris as well
  krpc_SpaceCenter_set_MapFilter(
      conn, filter | KRPC_SPACECENTER_MAPFILTERTYPE_DEBRIS);
  // [/docs]

  krpc_disconnect(conn);
}
// NOLINTEND(bugprone-signed-bitwise)
