#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "TargetPitchAndHeading") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  // [docs]
  krpc_SpaceCenter_AutoPilot_t ap;
  krpc_SpaceCenter_Vessel_AutoPilot(conn, &ap, vessel);
  krpc_SpaceCenter_ReferenceFrame_t frame;
  krpc_SpaceCenter_Vessel_SurfaceReferenceFrame(conn, &frame, vessel);
  krpc_SpaceCenter_AutoPilot_set_ReferenceFrame(conn, ap, frame);
  // 80 degrees above the horizon, facing east
  krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, ap, 80, 90);
  krpc_SpaceCenter_AutoPilot_set_Engaged(conn, ap, true);
  // blocks until the vessel has settled on the target
  krpc_SpaceCenter_AutoPilot_Wait(conn, ap, -1);
  krpc_SpaceCenter_AutoPilot_set_Engaged(conn, ap, false);
  // [/docs]

  krpc_disconnect(conn);
}
