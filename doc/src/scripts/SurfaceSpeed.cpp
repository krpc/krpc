#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect("Surface speed");
  krpc::services::SpaceCenter spaceCenter(&conn);
  auto vessel = spaceCenter.active_vessel();
  auto ref_frame = vessel.orbit().body().reference_frame();

  while (true) {
    auto velocity = vessel.flight(ref_frame).velocity();
    std::cout
      << "Surface velocity = ("
      << std::get<0>(velocity) << ","
      << std::get<1>(velocity) << ","
      << std::get<2>(velocity)
      << ")" << std::endl;

    auto speed = vessel.flight(ref_frame).speed();
    std::cout << "Surface speed = " << speed << " m/s" << std::endl;

    std::this_thread::sleep_for(std::chrono::seconds(1));
  }
}
