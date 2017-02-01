#include <iostream>
#include <chrono>
#include <thread>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect("Sub-orbital flight");
  krpc::services::SpaceCenter space_center(&conn);

  auto vessel = space_center.active_vessel();

  vessel.auto_pilot().target_pitch_and_heading(90, 90);
  vessel.auto_pilot().engage();
  vessel.control().set_throttle(1);
  std::this_thread::sleep_for(std::chrono::seconds(1));

  std::cout << "Launch!" << std::endl;
  vessel.control().activate_next_stage();

  while (vessel.resources().amount("SolidFuel") > 0.1)
    std::this_thread::sleep_for(std::chrono::seconds(1));
  std::cout << "Booster separation" << std::endl;
  vessel.control().activate_next_stage();

  while (vessel.flight().mean_altitude() < 10000)
    std::this_thread::sleep_for(std::chrono::seconds(1));

  std::cout << "Gravity turn" << std::endl;
  vessel.auto_pilot().target_pitch_and_heading(60, 90);

  while (vessel.orbit().apoapsis_altitude() < 100000)
    std::this_thread::sleep_for(std::chrono::seconds(1));
  std::cout << "Launch stage separation" << std::endl;
  vessel.control().set_throttle(0);
  std::this_thread::sleep_for(std::chrono::seconds(1));
  vessel.control().activate_next_stage();
  vessel.auto_pilot().disengage();

  while (vessel.flight().surface_altitude() > 1000)
    std::this_thread::sleep_for(std::chrono::seconds(1));
  vessel.control().activate_next_stage();

  while (vessel.flight(vessel.orbit().body().reference_frame()).vertical_speed() < -0.1) {
    std::cout << "Altitude = " << vessel.flight().surface_altitude() << " meters" << std::endl;
    std::this_thread::sleep_for(std::chrono::seconds(1));
  }
  std::cout << "Landed!" << std::endl;
}
