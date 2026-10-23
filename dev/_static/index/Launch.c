#include <stdio.h>
#include <stdlib.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Launch");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_AutoPilot_t auto_pilot;
  krpc_SpaceCenter_Vessel_AutoPilot(conn, &auto_pilot, vessel);
  krpc_SpaceCenter_Control_t control;
  krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  char * name = NULL;
  krpc_SpaceCenter_Vessel_Name(conn, &name, vessel);

  krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, auto_pilot, 90, 90);
  krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, true);
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 1);
  krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);
  printf("%s has launched\n", name);

  double mean_altitude = 0;
  while (mean_altitude < 10000)
    krpc_SpaceCenter_Flight_MeanAltitude(conn, &mean_altitude, flight);

  krpc_SpaceCenter_Control_set_Throttle(conn, control, 0);
  krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, false);
  printf("%s reached 10 km\n", name);
  free(name);
}
