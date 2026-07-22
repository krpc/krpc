#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel();
  vessel.set_name("My Vessel");
  auto flight_info = vessel.flight();
  std::cout << flight_info.mean_altitude() << std::endl;
}
