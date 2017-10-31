#include <krpc.h>
#include <krpc/services/space_center.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "DeployParachutes");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);

  krpc_list_object_t parachutes;
  krpc_SpaceCenter_Parts_Parachutes(conn, &parachutes, parts);
  for (size_t i = 0; i < parachutes.size; i++) {
    krpc_SpaceCenter_Parachute_t parachute = parachutes.items[i];
    krpc_SpaceCenter_Parachute_Deploy(conn, parachute);
  }
}
