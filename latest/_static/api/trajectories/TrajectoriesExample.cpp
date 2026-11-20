#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/trajectories.hpp>

int main() {
  auto conn = krpc::connect("Trajectories Example");
  krpc::services::SpaceCenter space_center(conn);
  krpc::services::Trajectories trajectories(conn);
  auto body = space_center.active_vessel().value().orbit().body();
  auto position = trajectories.impact_position(body.reference_frame());
  if (!position) {
    std::cout << "No impact" << std::endl;
    return 0;
  }
  auto [x, y, z] = *position;
  std::cout << x << ", " << y << ", " << z << std::endl;
}
