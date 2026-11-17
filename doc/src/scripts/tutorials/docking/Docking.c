#include <krpc_cnano.h>
#include <krpc_cnano/services/drawing.h>
#include <krpc_cnano/services/space_center.h>
#include <krpc_cnano/services/std_lib.h>
#include <math.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Docking") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  krpc_SpaceCenter_Part_t controlling;
  krpc_SpaceCenter_Parts_Controlling(conn, &controlling, parts);
  krpc_SpaceCenter_DockingPort_t port;
  krpc_SpaceCenter_Part_DockingPort(conn, &port, controlling);
  krpc_SpaceCenter_DockingPort_t target;
  krpc_SpaceCenter_TargetDockingPort(conn, &target);
  krpc_SpaceCenter_ReferenceFrame_t frame;
  krpc_SpaceCenter_DockingPort_ReferenceFrame(conn, &frame, target);
  krpc_SpaceCenter_ReferenceFrame_t vessel_frame;
  krpc_SpaceCenter_Vessel_ReferenceFrame(conn, &vessel_frame, vessel);
  krpc_SpaceCenter_Control_t control;
  krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);
  krpc_SpaceCenter_AutoPilot_t auto_pilot;
  krpc_SpaceCenter_Vessel_AutoPilot(conn, &auto_pilot, vessel);

  // Turn to face the target port
  printf("Turning to face the target\n");
  krpc_SpaceCenter_Control_set_SAS(conn, control, false);
  krpc_SpaceCenter_Control_set_RCS(conn, control, true);
  krpc_SpaceCenter_AutoPilot_set_ReferenceFrame(conn, auto_pilot, frame);
  krpc_tuple_double_double_double_t facing = {0, -1, 0};
  krpc_SpaceCenter_AutoPilot_set_TargetDirection(conn, auto_pilot, &facing);
  krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, true);
  krpc_SpaceCenter_AutoPilot_Wait(conn, auto_pilot, -1);

  // Draw the docking axis, and a line from our port to the target
  krpc_tuple_double_double_double_t origin = {0, 0, 0};
  krpc_tuple_double_double_double_t axis_end = {0, 50, 0};
  krpc_Drawing_Line_t axis;
  krpc_Drawing_AddLine(conn, &axis, &origin, &axis_end, frame, true);
  krpc_StdLib_ColorRGB_t green = {0, 1, 0};
  krpc_Drawing_Line_set_Color(conn, axis, &green);
  krpc_Drawing_Line_t approach;
  krpc_Drawing_AddLine(conn, &approach, &origin, &origin, frame, true);
  krpc_StdLib_ColorRGB_t yellow = {1, 1, 0};
  krpc_Drawing_Line_set_Color(conn, approach, &yellow);

  // Approach the target port along its axis
  printf("Approaching\n");
  while (true) {
    krpc_SpaceCenter_DockingPortState_t state;
    krpc_SpaceCenter_DockingPort_State(conn, &state, port);
    if (state == KRPC_SPACECENTER_DOCKINGPORTSTATE_DOCKING ||
        state == KRPC_SPACECENTER_DOCKINGPORTSTATE_DOCKED)
      break;
    krpc_tuple_double_double_double_t p;
    krpc_SpaceCenter_DockingPort_Position(conn, &p, port, frame);
    krpc_Drawing_Line_set_Start(conn, approach, &p);

    // Line up on the axis, then close in along it
    double off_axis = hypot(p.e0, p.e2);
    double goal_y = off_axis < 0.5 ? 0 : fmax(p.e1, 5);
    double ex = -p.e0, ey = goal_y - p.e1, ez = -p.e2;
    double distance = sqrt(ex * ex + ey * ey + ez * ez);
    double speed = fmin(1, fmax(0.1, 0.2 * distance));
    double scale = speed / fmax(distance, 0.01);

    // Thrust towards the velocity we want, in the vessel's frame
    krpc_tuple_double_double_double_t v;
    krpc_SpaceCenter_Vessel_Velocity(conn, &v, vessel, frame);
    krpc_tuple_double_double_double_t thrust = {2 * (ex * scale - v.e0),
                                                2 * (ey * scale - v.e1),
                                                2 * (ez * scale - v.e2)};
    krpc_tuple_double_double_double_t t;
    krpc_SpaceCenter_TransformDirection(conn, &t, &thrust, frame,
                                        vessel_frame);
    krpc_SpaceCenter_Control_set_Right(conn, control,
                                       fmin(1, fmax(-1, t.e0)));
    krpc_SpaceCenter_Control_set_Forward(conn, control,
                                         fmin(1, fmax(-1, t.e1)));
    krpc_SpaceCenter_Control_set_Up(conn, control,
                                    fmin(1, fmax(-1, -t.e2)));
  }

  // Stop the thrusters, and remove the drawing
  krpc_SpaceCenter_Control_set_Right(conn, control, 0);
  krpc_SpaceCenter_Control_set_Forward(conn, control, 0);
  krpc_SpaceCenter_Control_set_Up(conn, control, 0);
  krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, false);
  krpc_Drawing_Clear(conn, false);
  printf("Docked\n");

  krpc_disconnect(conn);
}
