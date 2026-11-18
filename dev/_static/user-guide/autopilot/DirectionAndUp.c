#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <math.h>

static const double pi = 3.1415926535897;

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "DirectionAndUp") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  // [docs]
  krpc_SpaceCenter_AutoPilot_t ap;
  krpc_SpaceCenter_Vessel_AutoPilot(conn, &ap, vessel);
  krpc_SpaceCenter_ReferenceFrame_t frame;
  krpc_SpaceCenter_Vessel_SurfaceReferenceFrame(conn, &frame, vessel);
  krpc_SpaceCenter_AutoPilot_set_ReferenceFrame(conn, ap, frame);
  // north, in the surface frame, off the flight path
  krpc_tuple_double_double_double_t up = {0, 1, 0};

  // straight up at the pad, roof toward north
  krpc_tuple_double_double_double_t vertical = {1, 0, 0};
  krpc_SpaceCenter_AutoPilot_SetDirectionAndUp(conn, ap, &vertical, &up, 0);
  krpc_SpaceCenter_AutoPilot_set_Engaged(conn, ap, true);
  krpc_SpaceCenter_AutoPilot_Wait(conn, ap, -1);

  // ease over to the eastern horizon; roll stays defined through vertical
  for (int i = 1; i <= 90; i++) {
    double pitch = (90 - i) * pi / 180;  // 89 down to 0 degrees
    krpc_tuple_double_double_double_t direction = {sin(pitch), 0,
                                                   cos(pitch)};
    krpc_SpaceCenter_AutoPilot_SetDirectionAndUp(conn, ap, &direction, &up,
                                                 0);
    krpc_SpaceCenter_AutoPilot_Wait(conn, ap, -1);
  }
  // [/docs]

  krpc_disconnect(conn);
}
