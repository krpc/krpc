#include <unistd.h>
#include <math.h>
#include <krpc.h>
#include <krpc/services/space_center.h>

typedef krpc_tuple_double_double_double_t vector3;

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Vessel velocity");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);

  krpc_SpaceCenter_ReferenceFrame_t vessel_srf_frame;
  krpc_SpaceCenter_Vessel_SurfaceReferenceFrame(conn, &vessel_srf_frame, vessel);
  krpc_SpaceCenter_ReferenceFrame_t body_frame;
  krpc_SpaceCenter_CelestialBody_ReferenceFrame(conn, &body_frame, body);

  krpc_SpaceCenter_ReferenceFrame_t ref_frame;
  krpc_SpaceCenter_ReferenceFrame_CreateHybrid(
    conn, &ref_frame, body_frame, vessel_srf_frame, KRPC_NULL, KRPC_NULL);

  while (true) {
    krpc_tuple_double_double_double_t velocity;
    krpc_SpaceCenter_Flight_t flight;
    krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, ref_frame);
    krpc_SpaceCenter_Flight_Velocity(conn, &velocity, flight);
    printf("Surface velocity = %.1f, %.1f, %.1f\n", velocity.e0, velocity.e1, velocity.e2);
    sleep(1);
  }
}
