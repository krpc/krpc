#include <iostream>
#include <iomanip>
#include <tuple>
#include <thread>
#include <chrono>
#include <cmath>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

static const double pi = 3.1415926535897;
typedef std::tuple<double, double, double> vector3;

vector3 cross_product(const vector3& u, const vector3& v) {
  return std::make_tuple(
    std::get<1>(u)*std::get<2>(v) - std::get<2>(u)*std::get<1>(v),
    std::get<2>(u)*std::get<0>(v) - std::get<0>(u)*std::get<2>(v),
    std::get<0>(u)*std::get<1>(v) - std::get<1>(u)*std::get<0>(v));
}

double dot_product(const vector3& u, const vector3& v) {
  return
    std::get<0>(u)*std::get<0>(v) +
    std::get<1>(u)*std::get<1>(v) +
    std::get<2>(u)*std::get<2>(v);
}

double magnitude(const vector3& v) {
  return std::sqrt(dot_product(v, v));
}

// Compute the angle between vector u and v
double angle_between_vectors(const vector3& u, const vector3& v) {
  double dp = dot_product(u, v);
  if (dp == 0)
    return 0;
  double um = magnitude(u);
  double vm = magnitude(v);
  return std::acos(dp / (um*vm)) * (180.0 / pi);
}

int main() {
  krpc::Client conn = krpc::connect("Pitch/Heading/Roll");
  krpc::services::SpaceCenter space_center(&conn);
  auto vessel = space_center.active_vessel();

  while (true) {
    vector3 vessel_direction = vessel.direction(vessel.surface_reference_frame());

    // Get the direction of the vessel in the horizon plane
    vector3 horizon_direction {
      0, std::get<1>(vessel_direction), std::get<2>(vessel_direction)
    };

    // Compute the pitch - the angle between the vessels direction
    // and the direction in the horizon plane
    double pitch = angle_between_vectors(vessel_direction, horizon_direction);
    if (std::get<0>(vessel_direction) < 0)
      pitch = -pitch;

    // Compute the heading - the angle between north
    // and the direction in the horizon plane
    vector3 north {0, 1, 0};
    double heading = angle_between_vectors(north, horizon_direction);
    if (std::get<2>(horizon_direction) < 0)
      heading = 360 - heading;

    // Compute the roll
    // Compute the plane running through the vessels direction
    // and the upwards direction
    vector3 up {1, 0, 0};
    vector3 plane_normal = cross_product(vessel_direction, up);
    // Compute the upwards direction of the vessel
    vector3 vessel_up = space_center.transform_direction(
      std::make_tuple(0, 0, -1),
      vessel.reference_frame(),
      vessel.surface_reference_frame());
    // Compute the angle between the upwards direction of
    // the vessel and the plane normal
    double roll = angle_between_vectors(vessel_up, plane_normal);
    // Adjust so that the angle is between -180 and 180 and
    // rolling right is +ve and left is -ve
    if (std::get<0>(vessel_up) > 0)
        roll *= -1;
    else if (roll < 0)
        roll += 180;
    else
        roll -= 180;

    std::cout << std::fixed << std::setprecision(1);
    std::cout << "pitch = " << pitch << ", "
              << "heading = " << heading << ", "
              << "roll = " << roll << std::endl;

    std::this_thread::sleep_for(std::chrono::seconds(1));
  }
}
