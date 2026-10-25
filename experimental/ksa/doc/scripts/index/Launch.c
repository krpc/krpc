#include <stdio.h>
#include <stdlib.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/ksa.h>

int main() {
  krpc_connection_t conn;
  krpc_connection_config_t config = {"127.0.0.1", 50000};
  krpc_open(&conn, &config);
  krpc_connect(conn, "Launch");

  krpc_KSA_Vehicle_t vehicle;
  krpc_KSA_LaunchVehicle(conn, &vehicle, "Rocket", "CCSFS LC-39A");
  krpc_KSA_AutoPilot_t auto_pilot;
  krpc_KSA_Vehicle_AutoPilot(conn, &auto_pilot, vehicle);
  krpc_KSA_Control_t control;
  krpc_KSA_Vehicle_Control(conn, &control, vehicle);
  krpc_KSA_Flight_t flight;
  krpc_KSA_Vehicle_Flight(conn, &flight, vehicle);
  char * name = NULL;
  krpc_KSA_Vehicle_Name(conn, &name, vehicle);

  krpc_KSA_AutoPilot_TargetPitchAndHeading(conn, auto_pilot, 90, 90);
  krpc_KSA_AutoPilot_Engage(conn, auto_pilot);
  krpc_KSA_Control_set_Throttle(conn, control, 1);
  krpc_KSA_Control_ActivateNextSequence(conn, NULL, control);
  krpc_KSA_Control_set_EngineOn(conn, control, true);
  printf("%s has launched\n", name);

  double mean_altitude = 0;
  while (mean_altitude < 10000)
    krpc_KSA_Flight_MeanAltitude(conn, &mean_altitude, flight);

  krpc_KSA_Control_set_EngineOn(conn, control, false);
  krpc_KSA_AutoPilot_Disengage(conn, auto_pilot);
  printf("%s reached 10 km\n", name);
  free(name);
}
