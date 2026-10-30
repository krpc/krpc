#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto flight_info = sc.active_vessel().value().flight();
  auto altitude = flight_info.mean_altitude_stream();
  while (true)
    std::cout << altitude() << std::endl;
}
