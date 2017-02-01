#include <iostream>
#include <iomanip>
#include <cmath>
#include <chrono>
#include <thread>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

static const double pi = 3.1415926535897;

int main() {
  krpc::Client conn = krpc::connect("Angle of attack");
  krpc::services::SpaceCenter space_center(&conn);
  auto vessel = space_center.active_vessel();

  while (true) {
    auto d = vessel.direction(vessel.orbit().body().reference_frame());
    auto v = vessel.velocity(vessel.orbit().body().reference_frame());

    // Compute the dot product of d and v
    double dotProd =
      std::get<0>(d)*std::get<0>(v) +
      std::get<1>(d)*std::get<1>(v) +
      std::get<2>(d)*std::get<2>(v);

    // Compute the magnitude of v
    double vMag = sqrt(
      std::get<0>(v)*std::get<0>(v) +
      std::get<1>(v)*std::get<1>(v) +
      std::get<2>(v)*std::get<2>(v));
    // Note: don't need to magnitude of d as it is a unit vector

    // Compute the angle between the vectors
    double angle = 0;
    if (dotProd > 0)
      angle = fabs(acos(dotProd / vMag) * (180.0 / pi));

    std::cout << "Angle of attack = "
              << std::fixed << std::setprecision(1)
              << angle << " degrees" << std::endl;

    std::this_thread::sleep_for(std::chrono::seconds(1));
  }
}
