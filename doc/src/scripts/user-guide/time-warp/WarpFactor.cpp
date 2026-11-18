#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  // [docs]
  std::cout << space_center.maximum_rails_warp_factor() << std::endl;
  space_center.set_rails_warp_factor(3);
  // [/docs]
}
