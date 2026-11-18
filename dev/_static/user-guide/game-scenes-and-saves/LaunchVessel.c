#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Launch vessel") != KRPC_OK) return 1;

  // [docs]
  krpc_list_string_t names = KRPC_NULL_LIST;
  krpc_SpaceCenter_LaunchableVessels(conn, &names, "VAB");
  for (size_t i = 0; i < names.size; i++) {
    printf("%s\n", names.items[i]);
    krpc_free(names.items[i]);
  }
  KRPC_FREE_LIST(names);
  krpc_SpaceCenter_LaunchVesselFromVAB(conn, "Kerbal X", true);
  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  char* name = NULL;
  krpc_SpaceCenter_Vessel_Name(conn, &name, vessel);
  printf("%s\n", name);
  krpc_free(name);
  // [/docs]

  krpc_disconnect(conn);
}
