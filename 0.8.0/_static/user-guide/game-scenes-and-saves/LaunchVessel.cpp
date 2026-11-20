#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  // [docs]
  for (const auto& name : space_center.launchable_vessels("VAB"))
    std::cout << name << std::endl;
  space_center.launch_vessel_from_vab("Kerbal X");
  std::cout << space_center.active_vessel().value().name() << std::endl;
  // [/docs]
}
