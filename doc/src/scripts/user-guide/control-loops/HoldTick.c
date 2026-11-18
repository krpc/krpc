#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/services/space_center.h>

static float control_law(float pitch) { return 0.05f * (45 - pitch); }

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Hold tick example") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, KRPC_NULL);
  krpc_SpaceCenter_Control_t control;
  krpc_SpaceCenter_Vessel_Control(conn, &control, vessel);
  // [docs]
  while (true) {
    if (krpc_KRPC_HoldTick(conn) != KRPC_OK) break;
    float pitch;
    bool ok =
        krpc_SpaceCenter_Flight_Pitch(conn, &pitch, flight) == KRPC_OK &&
        krpc_SpaceCenter_Control_set_Pitch(conn, control,
                                           control_law(pitch)) == KRPC_OK;
    krpc_KRPC_ReleaseTick(conn);
    if (!ok) break;
  }
  // [/docs]

  krpc_disconnect(conn);
}
