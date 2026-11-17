// [docs]
#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "pitch hold") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  krpc_SpaceCenter_Control_t control;
  krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);
  float target_pitch = 45;

  for (int i = 0; i < 500; i++) {
    float pitch;
    if (krpc_KRPC_NextTick(conn) != KRPC_OK ||
        krpc_SpaceCenter_Flight_Pitch(conn, &pitch, flight) != KRPC_OK)
      break;
    float command = 0.05f * (target_pitch - pitch);
    if (command > 1) command = 1;
    if (command < -1) command = -1;
    if (krpc_SpaceCenter_Control_set_Pitch(conn, control, command) !=
        KRPC_OK)
      break;
  }
  krpc_KRPC_ReleaseTick(conn);
  krpc_SpaceCenter_Control_set_Pitch(conn, control, 0);

  krpc_disconnect(conn);
}
// [/docs]
