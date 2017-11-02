#include <krpc.h>
#include <krpc/services/space_center.h>
#include <krpc/services/remote_tech.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "RemoteTech Example");
  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  // Set a dish target
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  krpc_list_object_t parts_with_title;
  krpc_SpaceCenter_Parts_WithTitle(conn, &parts_with_title, parts, "Reflectron KR-7");
  krpc_SpaceCenter_Part_t part = parts_with_title.items[0];

  krpc_RemoteTech_Antenna_t antenna;
  krpc_RemoteTech_Antenna(conn, &antenna, part);

  krpc_dictionary_string_object_t bodies = KRPC_NULL_DICTIONARY;
  krpc_SpaceCenter_Bodies(conn, &bodies);
  krpc_SpaceCenter_CelestialBody_t jool;
  for (size_t i = 0; i < bodies.size; i++)
    if (!strcmp(bodies.entries[i].key, "Jool"))
      jool = bodies.entries[i].value;

  krpc_RemoteTech_Antenna_set_TargetBody(conn, antenna, jool);

  // Get info about the vessels communications
  krpc_RemoteTech_Comms_t comms;
  krpc_RemoteTech_Comms(conn, &comms, vessel);
  double signal_delay;
  krpc_RemoteTech_Comms_SignalDelay(conn, &signal_delay, comms);
  printf("Signal delay = %.2f\n", signal_delay);
}
