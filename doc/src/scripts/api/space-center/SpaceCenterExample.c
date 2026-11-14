#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "SpaceCenter Example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  char* name = NULL;
  krpc_SpaceCenter_Vessel_Name(conn, &name, vessel);
  printf("%s\n", name);
  krpc_free(name);
  krpc_disconnect(conn);
}
