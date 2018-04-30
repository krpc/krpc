#include <unistd.h>
#include <math.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

typedef krpc_tuple_double_double_double_t vector3;

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Vessel speed");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);

  krpc_SpaceCenter_ReferenceFrame_t obt_frame;
  krpc_SpaceCenter_ReferenceFrame_t srf_frame;
  krpc_SpaceCenter_CelestialBody_NonRotatingReferenceFrame(conn, &obt_frame, body);
  krpc_SpaceCenter_CelestialBody_ReferenceFrame(conn, &srf_frame, body);

  while (true) {
    double obt_speed;
    double srf_speed;
    krpc_SpaceCenter_Flight_t flight;
    krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, obt_frame);
    krpc_SpaceCenter_Flight_Speed(conn, &obt_speed, flight);
    krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, srf_frame);
    krpc_SpaceCenter_Flight_Speed(conn, &srf_speed, flight);
    printf("Orbital speed = %.1f m/s, Surface speed = %.1f m/s\n", obt_speed, srf_speed);
    sleep(1);
  }
}
