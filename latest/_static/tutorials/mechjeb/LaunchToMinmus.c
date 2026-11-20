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
#include <krpc_cnano/services/mech_jeb.h>
#include <krpc_cnano/services/space_center.h>
#include <stdbool.h>
#include <stdio.h>
#include <string.h>

// Print the step of an autopilot's status, if it differs from the last
static void print_step(const char* status, char* last, size_t size) {
  // The step, without the figures that change on each update
  size_t length = strcspn(status, "\n0123456789");
  const char* infinity = strstr(status, "∞");
  if (infinity && (size_t)(infinity - status) < length)
    length = infinity - status;
  while (length > 0 && strchr(" :=", status[length - 1])) length--;
  if (length == 0 || length >= size) return;
  if (strncmp(status, last, length) == 0 && last[length] == '\0') return;
  memcpy(last, status, length);
  last[length] = '\0';
  printf("%s\n", last);
}

static void print_orbit(krpc_connection_t conn,
                        krpc_SpaceCenter_Vessel_t vessel) {
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);
  char* name = NULL;
  krpc_SpaceCenter_CelestialBody_Name(conn, &name, body);
  double apoapsis, periapsis;
  krpc_SpaceCenter_Orbit_ApoapsisAltitude(conn, &apoapsis, orbit);
  krpc_SpaceCenter_Orbit_PeriapsisAltitude(conn, &periapsis, orbit);
  printf("Orbit of %s, %.0f km by %.0f km\n", name, apoapsis / 1000,
         periapsis / 1000);
  krpc_free(name);
}

// Execute the vessel's maneuver nodes, and wait for the burns
static void execute_nodes(krpc_connection_t conn,
                          krpc_MechJeb_ManeuverPlanner_t planner) {
  krpc_MechJeb_ManeuverPlanner_ExecuteAllNodes(conn, planner);
  bool executing = true;
  while (executing) {
    sleep_ms(100);
    krpc_MechJeb_ManeuverPlanner_ExecutingNode(conn, &executing, planner);
  }
}

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Launch to Minmus") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Control_t control;
  krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);

  // The target orbit, set as in the Ascent Guidance window
  krpc_MechJeb_AscentGuidance_t ascent;
  krpc_MechJeb_AscentGuidance(conn, &ascent);
  krpc_MechJeb_AscentGuidance_set_AscentType(
      conn, ascent, KRPC_MECHJEB_ASCENTTYPE_CLASSIC);
  krpc_MechJeb_AscentGuidance_set_DesiredOrbitAltitude(conn, ascent,
                                                       100000);
  krpc_MechJeb_AscentGuidance_set_DesiredInclination(conn, ascent, 0);

  // The Ascent Settings window
  krpc_MechJeb_AscentSettings_t settings;
  krpc_MechJeb_AscentGuidance_Settings(conn, &settings, ascent);
  krpc_MechJeb_AscentSettings_set_Autostage(conn, settings, true);
  krpc_MechJeb_AscentSettings_set_LimitAngleOfAttack(conn, settings, true);
  krpc_MechJeb_AscentSettings_set_MaxAngleOfAttack(conn, settings, 5);
  krpc_MechJeb_AscentSettings_set_Autowarp(conn, settings, true);

  krpc_MechJeb_AscentGuidance_set_Enabled(conn, ascent, true);
  krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);

  // Wait for the autopilot to disengage, once the orbit is circular,
  // and print each of its steps
  char step[128] = "";
  bool enabled = true;
  while (enabled) {
    char* status = NULL;
    krpc_MechJeb_AscentGuidance_Status(conn, &status, ascent);
    print_step(status, step, sizeof(step));
    krpc_free(status);
    sleep_ms(100);
    krpc_MechJeb_AscentGuidance_Enabled(conn, &enabled, ascent);
  }
  print_orbit(conn, vessel);

  krpc_MechJeb_ManeuverPlanner_t planner;
  krpc_MechJeb_ManeuverPlanner(conn, &planner);
  krpc_MechJeb_ManeuverPlanner_set_Autowarp(conn, planner, true);

  // Stage once a stage burns out, during the burns too
  krpc_MechJeb_Utilities_t utilities;
  krpc_MechJeb_Utilities(conn, &utilities);
  krpc_MechJeb_Autostaging_t staging;
  krpc_MechJeb_Utilities_Staging(conn, &staging, utilities);
  krpc_MechJeb_Autostaging_set_Enabled(conn, staging, true);

  // Target Minmus, and wait for MechJeb to see the target
  krpc_dictionary_string_object_t bodies = KRPC_NULL_DICTIONARY;
  krpc_SpaceCenter_Bodies(conn, &bodies);
  krpc_SpaceCenter_CelestialBody_t minmus = KRPC_NULL;
  for (size_t i = 0; i < bodies.size; i++)
    if (!strcmp(bodies.entries[i].key, "Minmus"))
      minmus = bodies.entries[i].value;
  KRPC_FREE_DICTIONARY(bodies);
  krpc_SpaceCenter_set_TargetBody(conn, minmus);
  krpc_MechJeb_TargetController_t target_controller;
  krpc_MechJeb_TargetController(conn, &target_controller);
  bool target_exists = false;
  while (!target_exists) {
    sleep_ms(100);
    krpc_MechJeb_TargetController_NormalTargetExists(conn, &target_exists,
                                                     target_controller);
  }

  // Match planes with Minmus, at the nearest relative node
  krpc_list_object_t nodes = KRPC_NULL_LIST;
  krpc_MechJeb_OperationPlane_t plane;
  krpc_MechJeb_ManeuverPlanner_OperationPlane(conn, &plane, planner);
  krpc_MechJeb_TimeSelector_t time_selector;
  krpc_MechJeb_OperationPlane_TimeSelector(conn, &time_selector, plane);
  krpc_MechJeb_TimeSelector_set_TimeReference(
      conn, time_selector, KRPC_MECHJEB_TIMEREFERENCE_RELNEARESTAD);
  krpc_MechJeb_OperationPlane_MakeNodes(conn, &nodes, plane);
  KRPC_FREE_LIST(nodes);
  execute_nodes(conn, planner);

  // The Hohmann transfer, to an encounter with no capture burn
  krpc_MechJeb_OperationTransfer_t transfer;
  krpc_MechJeb_ManeuverPlanner_OperationTransfer(conn, &transfer, planner);
  krpc_MechJeb_OperationTransfer_TimeSelector(conn, &time_selector,
                                              transfer);
  krpc_MechJeb_TimeSelector_set_TimeReference(
      conn, time_selector, KRPC_MECHJEB_TIMEREFERENCE_COMPUTED);
  krpc_MechJeb_OperationTransfer_set_Capture(conn, transfer, false);
  krpc_MechJeb_OperationTransfer_MakeNodes(conn, &nodes, transfer);
  KRPC_FREE_LIST(nodes);
  execute_nodes(conn, planner);

  // Fine tune the encounter, for a periapsis 20 km above Minmus
  krpc_MechJeb_OperationCourseCorrection_t correction;
  krpc_MechJeb_ManeuverPlanner_OperationCourseCorrection(conn, &correction,
                                                         planner);
  krpc_MechJeb_OperationCourseCorrection_set_FinalPeriapsisAltitude(
      conn, correction, 20000);
  krpc_MechJeb_OperationCourseCorrection_MakeNodes(conn, &nodes,
                                                   correction);
  KRPC_FREE_LIST(nodes);
  execute_nodes(conn, planner);

  // Warp into Minmus' sphere of influence
  double ut, time_to_soi_change;
  krpc_SpaceCenter_UT(conn, &ut);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_Orbit_TimeToSOIChange(conn, &time_to_soi_change, orbit);
  krpc_SpaceCenter_WarpTo(conn, ut + time_to_soi_change + 60, 100000, 2);

  // Circularize at the periapsis
  krpc_MechJeb_OperationCircularize_t circularize;
  krpc_MechJeb_ManeuverPlanner_OperationCircularize(conn, &circularize,
                                                    planner);
  krpc_MechJeb_OperationCircularize_TimeSelector(conn, &time_selector,
                                                 circularize);
  krpc_MechJeb_TimeSelector_set_TimeReference(
      conn, time_selector, KRPC_MECHJEB_TIMEREFERENCE_PERIAPSIS);
  krpc_MechJeb_OperationCircularize_MakeNodes(conn, &nodes, circularize);
  KRPC_FREE_LIST(nodes);
  execute_nodes(conn, planner);

  krpc_SpaceCenter_ClearTarget(conn);
  print_orbit(conn, vessel);

  // Stage until the next stage would drop the landing legs. This drops
  // the stages below the lander, and activates the lander's engine.
  krpc_MechJeb_Autostaging_set_Enabled(conn, staging, false);
  krpc_SpaceCenter_Parts_t parts;
  krpc_SpaceCenter_Vessel_Parts(conn, &parts, vessel);
  krpc_list_object_t legs = KRPC_NULL_LIST;
  krpc_SpaceCenter_Parts_Legs(conn, &legs, parts);
  krpc_SpaceCenter_Part_t legs_part;
  krpc_SpaceCenter_Leg_Part(conn, &legs_part, legs.items[0]);
  KRPC_FREE_LIST(legs);
  int32_t legs_stage, current_stage;
  krpc_SpaceCenter_Part_DecoupleStage(conn, &legs_stage, legs_part);
  krpc_SpaceCenter_Control_CurrentStage(conn, &current_stage, control);
  while (current_stage > legs_stage + 1) {
    krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);
    krpc_SpaceCenter_Control_CurrentStage(conn, &current_stage, control);
  }

  // Limit the lander's thrust, so its lowest throttle leaves it falling
  krpc_list_object_t engines = KRPC_NULL_LIST;
  krpc_SpaceCenter_Parts_Engines(conn, &engines, parts);
  for (size_t i = 0; i < engines.size; i++)
    krpc_SpaceCenter_Engine_set_ThrustLimit(conn, engines.items[i], 0.3);
  KRPC_FREE_LIST(engines);

  // Land on Minmus' Great Flats, and wait for MechJeb to see the target
  krpc_MechJeb_LandingGuidance_t landing;
  krpc_MechJeb_LandingGuidance(conn, &landing);
  krpc_MechJeb_LandingGuidance_SetTarget(conn, landing, minmus, 0, -6);
  target_exists = false;
  while (!target_exists) {
    sleep_ms(100);
    krpc_MechJeb_TargetController_PositionTargetExists(conn, &target_exists,
                                                       target_controller);
  }

  // The settings of the Landing Guidance window
  krpc_MechJeb_LandingGuidance_set_TouchdownSpeed(conn, landing, 1);
  krpc_MechJeb_LandingGuidance_set_DeployGears(conn, landing, true);
  krpc_MechJeb_LandingGuidance_set_Autowarp(conn, landing, true);

  krpc_MechJeb_LandingGuidance_LandAtTarget(conn, landing);

  // Wait for the autopilot to disengage, once the vessel has landed, and
  // print each of its steps
  step[0] = '\0';
  enabled = true;
  while (enabled) {
    char* status = NULL;
    krpc_MechJeb_LandingGuidance_Status(conn, &status, landing);
    print_step(status, step, sizeof(step));
    krpc_free(status);
    sleep_ms(100);
    krpc_MechJeb_LandingGuidance_Enabled(conn, &enabled, landing);
  }

  krpc_SpaceCenter_ClearTarget(conn);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  double latitude, longitude;
  krpc_SpaceCenter_Flight_Latitude(conn, &latitude, flight);
  krpc_SpaceCenter_Flight_Longitude(conn, &longitude, flight);
  printf("Landed at latitude %.2f, longitude %.2f\n", latitude, longitude);

  krpc_disconnect(conn);
}
