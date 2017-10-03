#include <iostream>
#include <iomanip>
#include <thread>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect("Vessel speed");
  krpc::services::SpaceCenter spaceCenter(&conn);
  auto vessel = spaceCenter.active_vessel();
  auto obt_frame = vessel.orbit().body().non_rotating_reference_frame();
  auto srf_frame = vessel.orbit().body().reference_frame();

  while (true) {
    auto obt_speed = vessel.flight(obt_frame).speed();
    auto srf_speed = vessel.flight(srf_frame).speed();
    std::cout << std::fixed << std::setprecision(1)
              << "Orbital speed = " << obt_speed << " m/s, "
              << "Surface speed = " << srf_speed << " m/s" << std::endl;
    std::this_thread::sleep_for(std::chrono::seconds(1));
  }
}
