#include <math.h>
#include <unistd.h>
#include <krpc.h>
#include <krpc/services/space_center.h>

static double pi = 3.1415926535897;
typedef krpc_tuple_double_double_double_t vector3;

vector3 cross_product(vector3 u, vector3 v) {
  vector3 result = {
    u.e1*v.e2 - u.e2*v.e1,
    u.e2*v.e0 - u.e0*v.e2,
    u.e0*v.e1 - u.e1*v.e0
  };
  return result;
}

double dot_product(vector3 u, vector3 v) {
  return u.e0*v.e0 + u.e1*v.e1 + u.e2*v.e2;
}

double magnitude(vector3 v) {
  return sqrt(dot_product(v, v));
}

// Compute the angle between vector u and v
double angle_between_vectors(vector3 u, vector3 v) {
  double dp = dot_product(u, v);
  if (dp == 0)
    return 0;
  double um = magnitude(u);
  double vm = magnitude(v);
  return acos(dp / (um*vm)) * (180.0 / pi);
}

int main() {
  krpc_connection_t conn;
  krpc_open(&conn, "COM0");
  krpc_connect(conn, "");

  krpc_SpaceCenter_Vessel_t vessel;
  krpc_SpaceCenter_ActiveVessel(conn, &vessel);
  krpc_SpaceCenter_ReferenceFrame_t srf_ref;
  krpc_SpaceCenter_Vessel_SurfaceReferenceFrame(conn, &srf_ref, vessel);
  krpc_SpaceCenter_ReferenceFrame_t vessel_ref;
  krpc_SpaceCenter_Vessel_ReferenceFrame(conn, &vessel_ref, vessel);

  while (true) {
    vector3 vessel_direction;
    krpc_SpaceCenter_Vessel_Direction(conn, &vessel_direction, vessel, srf_ref);

    // Get the direction of the vessel in the horizon plane
    vector3 horizon_direction = {
      0, vessel_direction.e1, vessel_direction.e2
    };

    // Compute the pitch - the angle between the vessels direction
    // and the direction in the horizon plane
    double pitch = angle_between_vectors(vessel_direction, horizon_direction);
    if (vessel_direction.e0 < 0)
      pitch = -pitch;

    // Compute the heading - the angle between north
    // and the direction in the horizon plane
    vector3 north = {0, 1, 0};
    double heading = angle_between_vectors(north, horizon_direction);
    if (horizon_direction.e2 < 0)
      heading = 360 - heading;

    // Compute the roll
    // Compute the plane running through the vessels direction
    // and the upwards direction
    vector3 up = {1, 0, 0};
    vector3 plane_normal = cross_product(vessel_direction, up);
    // Compute the upwards direction of the vessel
    vector3 vessel_up;
    vector3 tmp = { 0, 0, -1 };
    krpc_SpaceCenter_TransformDirection(conn, &vessel_up, &tmp, vessel_ref, srf_ref);
    // Compute the angle between the upwards direction of
    // the vessel and the plane normal
    double roll = angle_between_vectors(vessel_up, plane_normal);
    // Adjust so that the angle is between -180 and 180 and
    // rolling right is +ve and left is -ve
    if (vessel_up.e0 > 0)
        roll *= -1;
    else if (roll < 0)
        roll += 180;
    else
        roll -= 180;

    printf("pitch = %.1f, heading = %.1f, roll = %.1f\n", pitch, heading, roll);
    sleep(1);
  }
}
