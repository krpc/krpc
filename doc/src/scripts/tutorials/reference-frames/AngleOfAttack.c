#include <unistd.h>
#include <math.h>
#include <krpc.h>
#include <krpc/services/space_center.h>

typedef krpc_tuple_double_double_double_t vector3;

static double pi = 3.1415926535897;

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Angle of attack");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);
  krpc_SpaceCenter_ReferenceFrame_t body_ref;
  krpc_SpaceCenter_CelestialBody_ReferenceFrame(conn, &body_ref, body);

  while (true) {
    vector3 d;
    vector3 v;
    krpc_SpaceCenter_Vessel_Direction(conn, &d, vessel, body_ref);
    krpc_SpaceCenter_Vessel_Velocity(conn, &d, vessel, body_ref);

    // Compute the dot product of d and v
    double dotProd = d.e0*v.e0 + d.e1*v.e1 + d.e2*v.e2;

    // Compute the magnitude of v
    double vMag = sqrt(v.e0*v.e0 + v.e1*v.e1 + v.e2*v.e2);
    // Note: don't need to magnitude of d as it is a unit vector

    // Compute the angle between the vectors
    double angle = 0;
    if (dotProd > 0)
      angle = fabs(acos(dotProd / vMag) * (180.0 / pi));

    printf("Angle of attack = %.1f degrees\n", angle);

    sleep(1);
  }
}
