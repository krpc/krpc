#include <unistd.h>
#include <math.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "Navball speed");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);

  krpc_SpaceCenter_ReferenceFrame_t obt_frame;
  krpc_SpaceCenter_ReferenceFrame_t srf_frame;
  krpc_SpaceCenter_Vessel_OrbitSpeedReferenceFrame(conn, &obt_frame, vessel);
  krpc_SpaceCenter_Vessel_SurfaceSpeedReferenceFrame(conn, &srf_frame, vessel);

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
