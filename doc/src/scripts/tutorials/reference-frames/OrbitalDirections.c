#include <unistd.h>
#include <math.h>
#include <krpc.h>
#include <krpc/services/space_center.h>

typedef krpc_tuple_double_double_double_t vector3;

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Orbital directions");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_ReferenceFrame_t vessel_obt_ref;
  krpc_SpaceCenter_Vessel_OrbitalReferenceFrame(conn, &vessel_obt_ref, vessel);

  krpc_SpaceCenter_AutoPilot_t ap;
  krpc_SpaceCenter_Vessel_AutoPilot(conn, &ap, vessel);
  krpc_SpaceCenter_AutoPilot_set_ReferenceFrame(conn, ap, vessel_obt_ref);
  krpc_SpaceCenter_AutoPilot_Engage(conn, ap);

  // Point the vessel in the prograde direction
  {
    krpc_tuple_double_double_double_t direction = { 0, 1, 0 };
    krpc_SpaceCenter_AutoPilot_set_TargetDirection(conn, ap, &direction);
    krpc_SpaceCenter_AutoPilot_Wait(conn, ap);
  }

  // Point the vessel in the orbit normal direction
  {
    krpc_tuple_double_double_double_t direction = { 0, 0, 1 };
    krpc_SpaceCenter_AutoPilot_set_TargetDirection(conn, ap, &direction);
    krpc_SpaceCenter_AutoPilot_Wait(conn, ap);
  }

  // Point the vessel in the orbit radial direction
  {
    krpc_tuple_double_double_double_t direction = { -1, 0, 0 };
    krpc_SpaceCenter_AutoPilot_set_TargetDirection(conn, ap, &direction);
    krpc_SpaceCenter_AutoPilot_Wait(conn, ap);
  }

  krpc_SpaceCenter_AutoPilot_Disengage(conn, ap);
}
