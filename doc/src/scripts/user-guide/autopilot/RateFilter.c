#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "RateFilter") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  // [docs]
  krpc_SpaceCenter_AutoPilot_t ap;
  krpc_SpaceCenter_Vessel_AutoPilot(conn, &ap, vessel);
  // notch a known 1.4 Hz pitch and yaw bending mode from the start
  krpc_SpaceCenter_AutoPilot_set_PitchYawRateFilterMode(
      conn, ap, KRPC_SPACECENTER_RATEFILTERMODE_NOTCH);
  krpc_SpaceCenter_AutoPilot_set_PitchYawOscillationFrequency(conn, ap,
                                                              1.4);
  // [/docs]

  krpc_disconnect(conn);
}
