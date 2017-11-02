#include <unistd.h>
#include <krpc.h>
#include <krpc/services/krpc.h>
#include <krpc/services/space_center.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Sub-orbital flight");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  krpc_SpaceCenter_AutoPilot_t auto_pilot;
  krpc_SpaceCenter_Vessel_AutoPilot(conn, &auto_pilot, vessel);

  krpc_SpaceCenter_AutoPilot_t control;
  krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);

  krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, auto_pilot, 90, 90);
  krpc_SpaceCenter_AutoPilot_Engage(conn, auto_pilot);
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 1);
  sleep(1);

  printf("Launch!\n");
  krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);

  krpc_SpaceCenter_Resources_t resources;
  krpc_SpaceCenter_Vessel_Resources(conn, &resources, vessel);
  while (true) {
    float solid_fuel;
    krpc_SpaceCenter_Resources_Amount(conn, &solid_fuel, resources, "SolidFuel");
    if (solid_fuel < 0.1)
      break;
  }

  printf("Booster separation\n");
  krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);

  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  while (true) {
    double mean_altitude;
    krpc_SpaceCenter_Flight_MeanAltitude(conn, &mean_altitude, flight);
    if (mean_altitude > 10000)
      break;
  }

  printf("Gravity turn\n");
  krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(conn, auto_pilot, 60, 90);

  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  while (true) {
    double apoapsis_altitude;
    krpc_SpaceCenter_Orbit_ApoapsisAltitude(conn, &apoapsis_altitude, orbit);
    if (apoapsis_altitude > 100000)
      break;
  }

  printf("Launch stage separation\n");
  krpc_SpaceCenter_Control_set_Throttle(conn, control, 0);
  sleep(1);
  krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);
  krpc_SpaceCenter_AutoPilot_Disengage(conn, auto_pilot);

  while (true) {
    double srf_altitude;
    krpc_SpaceCenter_Flight_SurfaceAltitude(conn, &srf_altitude, flight);
    if (srf_altitude < 1000)
      break;
  }

  krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);

  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);
  krpc_SpaceCenter_ReferenceFrame_t body_ref_frame;
  krpc_SpaceCenter_CelestialBody_ReferenceFrame(conn, &body_ref_frame, body);
  krpc_SpaceCenter_Flight_t body_flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &body_flight, vessel, body_ref_frame);

  while (true) {
    double vertical_speed;
    krpc_SpaceCenter_Flight_VerticalSpeed(conn, &vertical_speed, body_flight);
    if (vertical_speed > -0.1)
      break;
    double altitude;
    krpc_SpaceCenter_Flight_SurfaceAltitude(conn, &altitude, flight);
    printf("Altitude = %2.f meters\n", altitude);
    sleep(1);
  }
  printf("Landed!\n");
}
