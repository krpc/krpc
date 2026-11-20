#include <krpc_cnano.h>
#include <krpc_cnano/services/debug.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Debug Example") != KRPC_OK) return 1;

  krpc_Debug_set_InfinitePropellant(conn, true);
  krpc_disconnect(conn);
}
