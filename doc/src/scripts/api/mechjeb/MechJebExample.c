#include <krpc_cnano.h>
#include <krpc_cnano/services/mech_jeb.h>
#include <krpc_cnano/services/space_center.h>
#include <stdbool.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "MechJeb Example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_MechJeb_AscentAutopilot_t ascent;
  krpc_MechJeb_AscentAutopilot(conn, &ascent);

  krpc_MechJeb_AscentAutopilot_set_DesiredOrbitAltitude(conn, ascent, 100000);
  krpc_MechJeb_AscentAutopilot_set_DesiredInclination(conn, ascent, 6);
  krpc_MechJeb_AscentAutopilot_set_Autostage(conn, ascent, true);
  krpc_MechJeb_AscentAutopilot_set_Enabled(conn, ascent, true);

  krpc_SpaceCenter_Control_t control;
  krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);
  krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);

  bool enabled = true;
  while (enabled) krpc_MechJeb_AscentAutopilot_Enabled(conn, &enabled, ascent);

  printf("Launch complete\n");
}
