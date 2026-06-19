#include <math.h>
#include <unistd.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Launch into orbit");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  float turn_start_altitude = 250;
  float turn_end_altitude = 45000;
  float target_altitude = 150000;

  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_Resources_t stage_2_resources;
  krpc_SpaceCenter_Vessel_ResourcesInDecoupleStage(conn, &stage_2_resources, vessel, 2, false);

  krpc_SpaceCenter_Control_t control;
  krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);
  krpc_SpaceCenter_AutoPilot_t auto_pilot;
  krpc_SpaceCenter_Vessel_AutoPilot(conn, &auto_pilot, vessel);

  krpc_SpaceCenter_Control_set_SAS(conn, control, false);
  krpc_SpaceCenter_Control_set_RCS(conn, control, false);
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 1);

  printf("3...\n");
  sleep(1);
  printf("2...\n");
  sleep(1);
  printf("1...\n");
  sleep(1);
  printf("Launch!\n");

  krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);
  krpc_SpaceCenter_AutoPilot_Engage(conn, auto_pilot);
  krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, auto_pilot, 90, 90);

  bool srbs_separated = false;
  double turn_angle = 0;
  while (true) {
    double altitude;
    krpc_SpaceCenter_Flight_MeanAltitude(conn, &altitude, flight);
    double apoapsis;
    krpc_SpaceCenter_Orbit_ApoapsisAltitude(conn, &apoapsis, orbit);

    if (altitude > turn_start_altitude && altitude < turn_end_altitude) {
      double frac = (altitude - turn_start_altitude) / (turn_end_altitude - turn_start_altitude);
      double new_turn_angle = frac * 90.0;
      if (fabs(new_turn_angle - turn_angle) > 0.5) {
        turn_angle = new_turn_angle;
        krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, auto_pilot, 90 - turn_angle, 90);
      }
    }

    if (!srbs_separated) {
      float srb_fuel;
      krpc_SpaceCenter_Resources_Amount(conn, &srb_fuel, stage_2_resources, "SolidFuel");
      if (srb_fuel < 0.1) {
        krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);
        srbs_separated = true;
        printf("SRBs separated\n");
      }
    }

    if (apoapsis > target_altitude * 0.9) {
      printf("Approaching target apoapsis\n");
      break;
    }
  }

  krpc_SpaceCenter_Control_set_Throttle(conn, control, 0.25);
  while (true) {
    double apoapsis;
    krpc_SpaceCenter_Orbit_ApoapsisAltitude(conn, &apoapsis, orbit);
    if (apoapsis >= target_altitude)
      break;
  }
  printf("Target apoapsis reached\n");
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 0);

  printf("Coasting out of atmosphere\n");
  while (true) {
    double altitude;
    krpc_SpaceCenter_Flight_MeanAltitude(conn, &altitude, flight);
    if (altitude >= 70500)
      break;
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
  krpc_SpaceCenter_Control_AddNode(conn, &node, control, ut + time_to_apoapsis, delta_v, 0, 0);

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
  krpc_SpaceCenter_ReferenceFrame_t node_ref;
  krpc_SpaceCenter_Node_ReferenceFrame(conn, &node_ref, node);
  krpc_SpaceCenter_AutoPilot_set_ReferenceFrame(conn, auto_pilot, node_ref);
  krpc_tuple_double_double_double_t burn_direction = {0, 1, 0};
  krpc_SpaceCenter_AutoPilot_set_TargetDirection(conn, auto_pilot, &burn_direction);
  krpc_SpaceCenter_AutoPilot_Wait(conn, auto_pilot);

  printf("Waiting until circularization burn\n");
  krpc_SpaceCenter_UT(conn, &ut);
  krpc_SpaceCenter_Orbit_TimeToApoapsis(conn, &time_to_apoapsis, orbit);
  double burn_ut = ut + time_to_apoapsis - (burn_time / 2.0);
  double lead_time = 5;
  krpc_SpaceCenter_WarpTo(conn, burn_ut - lead_time, 100000, 2);

  printf("Ready to execute burn\n");
  while (true) {
    krpc_SpaceCenter_UT(conn, &ut);
    krpc_SpaceCenter_Orbit_TimeToApoapsis(conn, &time_to_apoapsis, orbit);
    if (time_to_apoapsis - (burn_time / 2.0) <= 0)
      break;
  }
  printf("Executing burn\n");
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 1);
  sleep((unsigned int)(burn_time - 0.1));
  printf("Fine tuning\n");
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 0.05);
  while (true) {
    krpc_tuple_double_double_double_t remaining_burn;
    krpc_SpaceCenter_Node_RemainingBurnVector(conn, &remaining_burn, node, node_ref);
    if (remaining_burn.e1 <= 0)
      break;
  }
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 0);
  krpc_SpaceCenter_Node_Remove(conn, node);

  printf("Launch complete\n");
}
