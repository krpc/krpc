#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "CenterOfBuoyancy") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  // [docs]
  krpc_SpaceCenter_ReferenceFrame_t frame;
  krpc_SpaceCenter_Vessel_SurfaceReferenceFrame(conn, &frame, vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, frame);
  krpc_tuple_double_double_double_t center_of_buoyancy, center_of_mass;
  bool out_of_water;
  krpc_SpaceCenter_Flight_CenterOfBuoyancy(conn, &center_of_buoyancy,
                                           &out_of_water, flight);
  krpc_SpaceCenter_Flight_CenterOfMass(conn, &center_of_mass, flight);
  // [/docs]
  if (!out_of_water)
    printf("Trim north = %.2f m, east = %.2f m\n",
           center_of_buoyancy.e1 - center_of_mass.e1,
           center_of_buoyancy.e2 - center_of_mass.e2);

  krpc_disconnect(conn);
}
