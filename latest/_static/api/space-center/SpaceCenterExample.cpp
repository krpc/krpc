#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect("SpaceCenter Example");
  krpc::services::SpaceCenter space_center(conn);
  std::cout << space_center.active_vessel().value().name() << std::endl;
}
