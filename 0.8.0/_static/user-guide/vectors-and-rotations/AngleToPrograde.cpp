#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/std_lib.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::StdLib std_lib(conn);
  krpc::services::SpaceCenter sc(conn);
  auto vessel = sc.active_vessel().value();
  auto frame = vessel.orbit().body().non_rotating_reference_frame();

  // [docs]
  auto direction = vessel.direction(frame);
  auto prograde = vessel.flight(frame).prograde();
  auto angle = std_lib.vector_angle(direction, prograde);
  std::cout << std_lib.radians_to_degrees(angle) << std::endl;
  // [/docs]
}
