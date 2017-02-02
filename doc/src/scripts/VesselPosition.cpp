#include <iostream>
#include <iomanip>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect();
  krpc::services::SpaceCenter spaceCenter(&conn);
  auto vessel = spaceCenter.active_vessel();
  auto position = vessel.position(vessel.orbit().body().reference_frame());
    std::cout << std::fixed << std::setprecision(1);
  std::cout << std::get<0>(position) << ", "
            << std::get<1>(position) << ", "
            << std::get<2>(position) << std::endl;
}
