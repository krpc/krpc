#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#define sleep_ms(ms) Sleep(ms)
#else
#define _POSIX_C_SOURCE 200809L
#include <time.h>
static void sleep_ms(long ms) {
  struct timespec duration = {ms / 1000, (ms % 1000) * 1000000};
  nanosleep(&duration, NULL);
}
#endif
#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Launch") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_AutoPilot_t auto_pilot;
  krpc_SpaceCenter_Vessel_AutoPilot(conn, &auto_pilot, vessel);
  krpc_SpaceCenter_Control_t control;
  krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  char* name = NULL;
  krpc_SpaceCenter_Vessel_Name(conn, &name, vessel);

  krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, auto_pilot, 90,
                                                   90);
  krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, true);
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 1);
  krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);
  printf("%s has launched\n", name);

  double mean_altitude = 0;
  while (mean_altitude < 10000) {
    krpc_SpaceCenter_Flight_MeanAltitude(conn, &mean_altitude, flight);
    sleep_ms(100);
  }

  krpc_SpaceCenter_Control_set_Throttle(conn, control, 0);
  krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, false);
  printf("%s reached 10 km\n", name);
  krpc_free(name);

  krpc_disconnect(conn);
}
