#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "PressureLimits") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  // [docs]
  krpc_list_object_t all = KRPC_NULL_LIST;
  krpc_SpaceCenter_Parts_All(conn, &all, parts);
  for (size_t i = 0; i < all.size; i++) {
    krpc_SpaceCenter_Part_t part = all.items[i];
    float static_pressure, dynamic_pressure;
    double max_pressure;
    krpc_SpaceCenter_Part_StaticPressure(conn, &static_pressure, part);
    krpc_SpaceCenter_Part_DynamicPressure(conn, &dynamic_pressure, part);
    krpc_SpaceCenter_Part_MaxPressure(conn, &max_pressure, part);
    if (static_pressure + dynamic_pressure > max_pressure) {
      char* title = NULL;
      krpc_SpaceCenter_Part_Title(conn, &title, part);
      printf("%s is over its pressure limit\n", title);
      krpc_free(title);
    }
  }
  KRPC_FREE_LIST(all);
  // [/docs]

  krpc_disconnect(conn);
}
