#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

using SpaceCenter = krpc::services::SpaceCenter;

int main() {
  krpc::Client conn = krpc::connect("Vessel Name");
  SpaceCenter sc(&conn);
  SpaceCenter::Vessel vessel = sc.active_vessel();
  std::cout << vessel.name() << std::endl;
}
