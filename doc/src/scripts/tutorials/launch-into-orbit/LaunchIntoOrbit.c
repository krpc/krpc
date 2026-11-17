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
#include <math.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Launch into orbit") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  float turn_start_altitude = 250;
  float turn_end_altitude = 45000;
  float target_altitude = 150000;

  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_Stage_t stage_5;
  krpc_SpaceCenter_Vessel_DecoupleStageAt(conn, &stage_5, vessel, 5);
  krpc_SpaceCenter_Resources_t stage_5_resources;
  krpc_SpaceCenter_Stage_Resources(conn, &stage_5_resources, stage_5,
                                   false);
  krpc_SpaceCenter_Stage_t stage_3;
  krpc_SpaceCenter_Vessel_DecoupleStageAt(conn, &stage_3, vessel, 3);
  krpc_SpaceCenter_Resources_t stage_3_resources;
  krpc_SpaceCenter_Stage_Resources(conn, &stage_3_resources, stage_3,
                                   false);

  krpc_SpaceCenter_Control_t control;
  krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);
  krpc_SpaceCenter_AutoPilot_t auto_pilot;
  krpc_SpaceCenter_Vessel_AutoPilot(conn, &auto_pilot, vessel);

  krpc_SpaceCenter_Control_set_SAS(conn, control, false);
  krpc_SpaceCenter_Control_set_RCS(conn, control, false);
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 1);

  krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, true);
  krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, auto_pilot, 90,
                                                   90);
  krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);

  bool first_stage_separated = false;
  bool second_stage_separated = false;
  while (true) {
    double altitude;
    krpc_SpaceCenter_Flight_MeanAltitude(conn, &altitude, flight);
    double apoapsis;
    krpc_SpaceCenter_Orbit_ApoapsisAltitude(conn, &apoapsis, orbit);

    if (altitude > turn_start_altitude && altitude < turn_end_altitude) {
      double frac = (altitude - turn_start_altitude) /
                    (turn_end_altitude - turn_start_altitude);
      double turn_angle = frac * 90.0;
      krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, auto_pilot,
                                                       90 - turn_angle, 90);
    }

    if (!first_stage_separated) {
      float fuel;
      krpc_SpaceCenter_Resources_Amount(conn, &fuel, stage_5_resources,
                                        "SolidFuel");
      if (fuel < 0.1) {
        krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);
        krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);
        first_stage_separated = true;
        printf("First stage separated\n");
      }
    } else if (!second_stage_separated) {
      float fuel;
      krpc_SpaceCenter_Resources_Amount(conn, &fuel, stage_3_resources,
                                        "LiquidFuel");
      if (fuel < 0.1) {
        krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);
        krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);
        second_stage_separated = true;
        printf("Second stage separated\n");
      }
    }

    if (apoapsis > target_altitude * 0.9) {
      printf("Approaching target apoapsis\n");
      break;
    }
    sleep_ms(50);
  }

  krpc_SpaceCenter_Control_set_Throttle(conn, control, 0.25);
  while (true) {
    double apoapsis;
    krpc_SpaceCenter_Orbit_ApoapsisAltitude(conn, &apoapsis, orbit);
    if (apoapsis >= target_altitude) break;
    sleep_ms(50);
  }
  printf("Target apoapsis reached\n");
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 0);

  printf("Coasting out of atmosphere\n");
  while (true) {
    double altitude;
    krpc_SpaceCenter_Flight_MeanAltitude(conn, &altitude, flight);
    if (altitude >= 70500) break;
    sleep_ms(50);
  }

  printf("Planning circularization burn\n");
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);
  double mu;
  krpc_SpaceCenter_CelestialBody_GravitationalParameter(conn, &mu, body);
  double r;
  krpc_SpaceCenter_Orbit_Apoapsis(conn, &r, orbit);
  double a1;
  krpc_SpaceCenter_Orbit_SemiMajorAxis(conn, &a1, orbit);
  double a2 = r;
  double v1 = sqrt(mu * ((2.0 / r) - (1.0 / a1)));
  double v2 = sqrt(mu * ((2.0 / r) - (1.0 / a2)));
  double delta_v = v2 - v1;
  double ut;
  krpc_SpaceCenter_UT(conn, &ut);
  double time_to_apoapsis;
  krpc_SpaceCenter_Orbit_TimeToApoapsis(conn, &time_to_apoapsis, orbit);
  krpc_SpaceCenter_Node_t node;
  krpc_SpaceCenter_Control_AddNode(conn, &node, control,
                                   ut + time_to_apoapsis, delta_v, 0, 0);

  float F;
  krpc_SpaceCenter_Vessel_AvailableThrust(conn, &F, vessel);
  float isp;
  krpc_SpaceCenter_Vessel_SpecificImpulse(conn, &isp, vessel);
  double Isp = isp * 9.82;
  float m0;
  krpc_SpaceCenter_Vessel_Mass(conn, &m0, vessel);
  double m1 = m0 / exp(delta_v / Isp);
  double flow_rate = F / Isp;
  double burn_time = (m0 - m1) / flow_rate;

  printf("Orientating ship for circularization burn\n");
  krpc_SpaceCenter_ReferenceFrame_t frame;
  krpc_SpaceCenter_CelestialBody_NonRotatingReferenceFrame(conn, &frame,
                                                           body);
  krpc_SpaceCenter_AutoPilot_set_ReferenceFrame(conn, auto_pilot, frame);
  krpc_tuple_double_double_double_t remaining_burn;
  krpc_SpaceCenter_Node_RemainingBurnVector(conn, &remaining_burn, node,
                                            frame);
  krpc_SpaceCenter_AutoPilot_set_TargetDirection(conn, auto_pilot,
                                                 &remaining_burn);
  krpc_SpaceCenter_AutoPilot_Wait(conn, auto_pilot, -1);

  printf("Waiting until circularization burn\n");
  krpc_SpaceCenter_UT(conn, &ut);
  krpc_SpaceCenter_Orbit_TimeToApoapsis(conn, &time_to_apoapsis, orbit);
  double burn_ut = ut + time_to_apoapsis - (burn_time / 2.0);
  double lead_time = 5;
  krpc_SpaceCenter_WarpTo(conn, burn_ut - lead_time, 100000, 2);

  printf("Ready to execute burn\n");
  while (true) {
    krpc_SpaceCenter_Orbit_TimeToApoapsis(conn, &time_to_apoapsis, orbit);
    if (time_to_apoapsis - (burn_time / 2.0) <= 0) break;
    sleep_ms(50);
  }
  printf("Executing burn\n");
  double remaining_delta_v;
  krpc_SpaceCenter_Node_RemainingDeltaV(conn, &remaining_delta_v, node);
  double lowest_delta_v = remaining_delta_v;
  while (remaining_delta_v > 0.1) {
    // Steer along the remaining burn, which turns as the vessel moves
    // around its orbit
    krpc_SpaceCenter_Node_RemainingBurnVector(conn, &remaining_burn, node,
                                              frame);
    krpc_SpaceCenter_AutoPilot_set_TargetDirection(conn, auto_pilot,
                                                   &remaining_burn);
    // Full throttle, then throttle down for the last few m/s
    krpc_SpaceCenter_Control_set_Throttle(conn, control,
                                          remaining_delta_v > 5 ? 1 : 0.05);
    // Stop if the burn overshoots, as the remaining delta-v then grows
    if (remaining_delta_v > lowest_delta_v + 0.1) break;
    if (remaining_delta_v < lowest_delta_v)
      lowest_delta_v = remaining_delta_v;
    krpc_SpaceCenter_Node_RemainingDeltaV(conn, &remaining_delta_v, node);
    sleep_ms(50);
  }
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 0);
  krpc_SpaceCenter_Node_Remove(conn, node);

  printf("Launch complete\n");

  krpc_disconnect(conn);
}
