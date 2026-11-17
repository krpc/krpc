#include <krpc_cnano.h>
#include <krpc_cnano/services/space_center.h>
#include <math.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Buoyancy Trim") != KRPC_OK) return 1;

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_Orbit_t orbit;
  krpc_SpaceCenter_Vessel_Orbit(conn, &orbit, vessel);
  krpc_SpaceCenter_CelestialBody_t body;
  krpc_SpaceCenter_Orbit_Body(conn, &body, orbit);

  bool has_ocean;
  krpc_SpaceCenter_CelestialBody_HasOcean(conn, &has_ocean, body);
  if (!has_ocean) {
    char* name = NULL;
    krpc_SpaceCenter_CelestialBody_Name(conn, &name, body);
    fprintf(stderr, "%s has no ocean\n", name);
    krpc_free(name);
    krpc_disconnect(conn);
    return 1;
  }

  krpc_SpaceCenter_ReferenceFrame_t frame;
  krpc_SpaceCenter_Vessel_SurfaceReferenceFrame(conn, &frame, vessel);
  krpc_SpaceCenter_Flight_t flight;
  krpc_SpaceCenter_Vessel_Flight(conn, &flight, vessel, frame);

  // The fully submerged figures, which do not depend on where the vessel
  // is
  double density, gravity, buoyancy, displacement;
  krpc_SpaceCenter_CelestialBody_OceanDensity(conn, &density, body);
  krpc_SpaceCenter_CelestialBody_SurfaceGravity(conn, &gravity, body);
  krpc_SpaceCenter_Flight_BuoyantForceAt(conn, &buoyancy, flight, density,
                                         gravity);
  krpc_tuple_double_double_double_t torque;
  krpc_SpaceCenter_Flight_BuoyantTorqueAt(conn, &torque, flight, density,
                                          gravity);
  float mass;
  krpc_SpaceCenter_Vessel_Mass(conn, &mass, vessel);
  double weight = mass * gravity;
  krpc_SpaceCenter_Flight_Displacement(conn, &displacement, flight);

  printf("Displacement:  %.1f m^3\n", displacement);
  printf("Buoyancy:      %.0f N fully submerged\n", buoyancy);
  printf("Weight:        %.0f N\n", weight);
  printf("Floats:        %s\n", buoyancy > weight ? "true" : "false");
  printf("Torque:        %.0f N m fully submerged\n",
         sqrt(torque.e0 * torque.e0 + torque.e1 * torque.e1 +
              torque.e2 * torque.e2));

  // The trim, read in the surface reference frame. Its x axis points up,
  // so the y and z axes span the horizontal. A horizontal offset between
  // the two centers tips the hull until they line up again.
  krpc_tuple_double_double_double_t center_of_buoyancy;
  bool out_of_water;
  krpc_SpaceCenter_Flight_CenterOfBuoyancy(conn, &center_of_buoyancy,
                                           &out_of_water, flight);

  if (out_of_water) {
    printf("Trim:          vessel is out of the water\n");
  } else {
    krpc_tuple_double_double_double_t center_of_mass;
    krpc_SpaceCenter_Flight_CenterOfMass(conn, &center_of_mass, flight);
    double north = center_of_buoyancy.e1 - center_of_mass.e1;
    double east = center_of_buoyancy.e2 - center_of_mass.e2;
    double submerged;
    krpc_SpaceCenter_Flight_SubmergedPortion(conn, &submerged, flight);
    printf("Submerged:     %.0f%%\n", 100 * submerged);
    printf("Trim:          %.2f m horizontally\n",
           sqrt(north * north + east * east));
    printf("Trim north:    %+.2f m\n", north);
    printf("Trim east:     %+.2f m\n", east);
  }

  krpc_disconnect(conn);
}
