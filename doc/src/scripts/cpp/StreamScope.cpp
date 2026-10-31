#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel().value();
  auto flight_info = vessel.flight();
  {
    auto altitude = flight_info.mean_altitude_stream();
    for (int i = 0; i < 10; i++)
      std::cout << altitude() << std::endl;
  }
  // The stream was removed at the end of the block
}
