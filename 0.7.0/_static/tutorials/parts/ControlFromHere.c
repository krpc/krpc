#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "ControlFromHere");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);

  krpc_list_object_t docking_port_parts;
  krpc_SpaceCenter_Parts_WithTitle(conn, &docking_port_parts, parts, "Clamp-O-Tron Docking Port");
  krpc_object_t part = docking_port_parts.items[0];
  krpc_SpaceCenter_Parts_set_Controlling(conn, parts, part);
}
