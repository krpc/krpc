#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Quickload") != KRPC_OK) return 1;

  // [docs]
  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  krpc_list_object_t chutes = KRPC_NULL_LIST;
  krpc_SpaceCenter_Parts_WithName(conn, &chutes, parts, "parachuteSingle");
  krpc_SpaceCenter_Parachute_t parachute;
  krpc_SpaceCenter_Part_Parachute(conn, &parachute, chutes.items[0]);
  KRPC_FREE_LIST(chutes);
  krpc_SpaceCenter_Quickload(conn);
  // reads the parachute of the loaded game
  krpc_SpaceCenter_ParachuteState_t state;
  krpc_SpaceCenter_Parachute_State(conn, &state, parachute);
  printf("%d\n", (int)state);
  // [/docs]

  krpc_disconnect(conn);
}
