#include <chrono>
#include <iostream>
#include <thread>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel().value();
  auto flight_info = vessel.flight();

  // Each read gets the current altitude from the game
  std::cout << flight_info.mean_altitude() << std::endl;
  std::this_thread::sleep_for(std::chrono::seconds(1));
  std::cout << flight_info.mean_altitude() << std::endl;

  // Two objects for the same vessel are equal
  std::cout << (vessel == sc.active_vessel().value()) << std::endl;
}
