#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Destroyed part") != KRPC_OK) return 1;
  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  krpc_list_object_t all = KRPC_NULL_LIST;
  krpc_SpaceCenter_Parts_All(conn, &all, parts);
  krpc_SpaceCenter_Part_t part = all.items[0];
  KRPC_FREE_LIST(all);

  // [docs]
  double temperature;
  krpc_error_t error =
      krpc_SpaceCenter_Part_Temperature(conn, &temperature, part);
  if (error == KRPC_OK)
    printf("%f\n", temperature);
  else if (error == KRPC_ERROR_SERVER &&
           KRPC_IS_EXCEPTION(KRPC_EXCEPTION_KRPC_OBJECTDESTROYEDEXCEPTION))
    printf("that part is gone\n");
  else
    return 1;
  // [/docs]

  krpc_disconnect(conn);
}
