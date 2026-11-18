#include <chrono>
#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <thread>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto flight = space_center.active_vessel().value().flight();
  // [docs]
  auto altitude = flight.mean_altitude_stream();
  altitude.set_rate(10);
  while (true) {
    std::cout << altitude() << std::endl;
    std::this_thread::sleep_for(std::chrono::milliseconds(100));
  }
  // [/docs]
}
