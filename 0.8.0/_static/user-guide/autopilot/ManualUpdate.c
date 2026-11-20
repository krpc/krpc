#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

static float control_law(float pitch) {
  return pitch + 0.05f * (45 - pitch);
}

static void record(float error, float pitch) {
  printf("%f %f\n", error, pitch);
}

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "ManualUpdate") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_AutoPilot_t ap;
  krpc_SpaceCenter_Vessel_AutoPilot(conn, &ap, vessel);
  krpc_SpaceCenter_AutoPilot_set_Engaged(conn, ap, true);
  // [docs]
  krpc_SpaceCenter_Control_t control;
  krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);
  krpc_SpaceCenter_AutoPilot_set_UpdateMode(
      conn, ap, KRPC_SPACECENTER_AUTOPILOTUPDATEMODE_MANUAL);
  while (true) {
    if (krpc_KRPC_HoldTick(conn) != KRPC_OK) break;
    krpc_SpaceCenter_Flight_t flight;
    float pitch, error, control_pitch;
    bool ok =
        krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL) ==
            KRPC_OK &&
        krpc_SpaceCenter_Flight_Pitch(conn, &pitch, flight) == KRPC_OK &&
        krpc_SpaceCenter_AutoPilot_TargetPitchAndHeading(
            conn, ap, control_law(pitch), 90) == KRPC_OK &&
        krpc_SpaceCenter_AutoPilot_Update(conn, ap) == KRPC_OK &&
        krpc_SpaceCenter_AutoPilot_Error(conn, &error, ap) == KRPC_OK &&
        krpc_SpaceCenter_Control_Pitch(conn, &control_pitch, control) ==
            KRPC_OK;
    if (ok) record(error, control_pitch);
    krpc_KRPC_ReleaseTick(conn);
    if (!ok) break;
  }
  // [/docs]

  krpc_disconnect(conn);
}
