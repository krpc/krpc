#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "LimitAngularVelocity") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  // [docs]
  krpc_SpaceCenter_AutoPilot_t ap;
  krpc_SpaceCenter_Vessel_AutoPilot(conn, &ap, vessel);
  // limit the angular velocity by MaxAngularVelocity only
  krpc_SpaceCenter_AutoPilot_set_LimitAngularVelocity(conn, ap, false);
  krpc_tuple_double_double_double_t max = {0.3, 0.5, 0.3};
  krpc_SpaceCenter_AutoPilot_set_MaxAngularVelocity(conn, ap, &max);
  // [/docs]

  krpc_disconnect(conn);
}
