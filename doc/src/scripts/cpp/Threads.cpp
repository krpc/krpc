#include <iostream>
#include <thread>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel().value();
  auto flight_info = vessel.flight(vessel.orbit().body().reference_frame());

  // Both threads make calls over the same connection
  std::thread altitude([&flight_info] {
    for (int i = 0; i < 10; i++)
      std::cout << "Altitude: " << flight_info.mean_altitude() << std::endl;
  });
  std::thread speed([&flight_info] {
    for (int i = 0; i < 10; i++)
      std::cout << "Speed: " << flight_info.speed() << std::endl;
  });
  altitude.join();
  speed.join();
}
