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
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Sub-orbital flight") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  krpc_SpaceCenter_AutoPilot_t auto_pilot;
  krpc_SpaceCenter_Vessel_AutoPilot(conn, &auto_pilot, vessel);

  krpc_SpaceCenter_Control_t control;
  krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);

  krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, auto_pilot, 90,
                                                   90);
  krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, true);
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 0.5);
  sleep_ms(1000);

  printf("Launch!\n");
  krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);

  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  while (true) {
    double mean_altitude;
    krpc_SpaceCenter_Flight_MeanAltitude(conn, &mean_altitude, flight);
    if (mean_altitude > 500) break;
    sleep_ms(100);
  }

  printf("Pitching over\n");
  krpc_SpaceCenter_AutoPilot_set_TargetSmoothingTime(conn, auto_pilot, 10);
  krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, auto_pilot, 70,
                                                   90);

  krpc_SpaceCenter_Resources_t resources;
  krpc_SpaceCenter_Vessel_Resources(conn, &resources, vessel);
  while (true) {
    float liquid_fuel;
    krpc_SpaceCenter_Resources_Amount(conn, &liquid_fuel, resources,
                                      "LiquidFuel");
    if (liquid_fuel < 0.1) break;
    sleep_ms(100);
  }

  printf("Flame out\n");
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 0);
  krpc_SpaceCenter_ReferenceFrame_t velocity_frame;
  krpc_SpaceCenter_Vessel_SurfaceVelocityReferenceFrame(
      conn, &velocity_frame, vessel);
  krpc_SpaceCenter_AutoPilot_set_ReferenceFrame(conn, auto_pilot,
                                                velocity_frame);
  krpc_tuple_double_double_double_t prograde = {0, 1, 0};
  krpc_SpaceCenter_AutoPilot_set_TargetDirection(conn, auto_pilot,
                                                 &prograde);

  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);
  krpc_SpaceCenter_ReferenceFrame_t body_frame;
  krpc_SpaceCenter_CelestialBody_ReferenceFrame(conn, &body_frame, body);
  krpc_SpaceCenter_Flight_t body_flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &body_flight, vessel, body_frame);
  while (true) {
    double vertical_speed;
    krpc_SpaceCenter_Flight_VerticalSpeed(conn, &vertical_speed,
                                          body_flight);
    if (vertical_speed < 0) break;
    sleep_ms(100);
  }

  printf("Apoapsis reached, separating\n");
  krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);
  krpc_tuple_double_double_double_t retrograde = {0, -1, 0};
  krpc_SpaceCenter_AutoPilot_set_TargetDirection(conn, auto_pilot,
                                                 &retrograde);
  krpc_SpaceCenter_AutoPilot_Wait(conn, auto_pilot, -1);
  krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, false);

  while (true) {
    double surface_altitude;
    krpc_SpaceCenter_Flight_SurfaceAltitude(conn, &surface_altitude,
                                            flight);
    if (surface_altitude < 2000) break;
    sleep_ms(100);
  }

  printf("Deploying the parachute\n");
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  krpc_list_object_t parachutes = KRPC_NULL_LIST;
  krpc_SpaceCenter_Parts_Parachutes(conn, &parachutes, parts);
  krpc_SpaceCenter_Parachute_set_DeployAltitude(conn, parachutes.items[0],
                                                550);
  KRPC_FREE_LIST(parachutes);
  krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);

  krpc_SpaceCenter_VesselSituation_t situation;
  while (true) {
    krpc_SpaceCenter_Vessel_Situation(conn, &situation, vessel);
    if (situation == KRPC_SPACECENTER_VESSELSITUATION_LANDED ||
        situation == KRPC_SPACECENTER_VESSELSITUATION_SPLASHED)
      break;
    double altitude;
    krpc_SpaceCenter_Flight_SurfaceAltitude(conn, &altitude, flight);
    printf("Altitude = %.1f meters\n", altitude);
    sleep_ms(1000);
  }
  printf("Landed!\n");

  krpc_disconnect(conn);
}
