#include <algorithm>
#include <chrono>
#include <cmath>
#include <iostream>
#include <krpc.hpp>
#include <krpc/services/drawing.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/std_lib.hpp>
#include <thread>
#include <tuple>

using SpaceCenter = krpc::services::SpaceCenter;
using Drawing = krpc::services::Drawing;
using ColorRGB = krpc::services::StdLib::ColorRGB;

int main() {
  krpc::Connection conn = krpc::connect("Docking");
  SpaceCenter space_center(conn);
  Drawing drawing(conn);
  auto vessel = space_center.active_vessel().value();
  auto port = vessel.parts().controlling()->docking_port().value();
  auto target = space_center.target_docking_port().value();
  auto frame = target.reference_frame();

  // Turn to face the target port
  std::cout << "Turning to face the target" << std::endl;
  vessel.control().set_sas(false);
  vessel.control().set_rcs(true);
  vessel.auto_pilot().set_reference_frame(frame);
  vessel.auto_pilot().set_target_direction(std::make_tuple(0.0, -1.0, 0.0));
  vessel.auto_pilot().set_engaged(true);
  vessel.auto_pilot().wait();

  // Draw the docking axis, and a line from our port to the target
  auto origin = std::make_tuple(0.0, 0.0, 0.0);
  auto axis =
      drawing.add_line(origin, std::make_tuple(0.0, 50.0, 0.0), frame);
  axis.set_color(ColorRGB(0, 1, 0));
  auto approach = drawing.add_line(origin, origin, frame);
  approach.set_color(ColorRGB(1, 1, 0));

  // Approach the target port along its axis
  std::cout << "Approaching" << std::endl;
  auto position = port.position_stream(frame);
  auto velocity = vessel.velocity_stream(frame);
  while (port.state() != SpaceCenter::DockingPortState::docking &&
         port.state() != SpaceCenter::DockingPortState::docked) {
    auto [x, y, z] = position();
    approach.set_start(std::make_tuple(x, y, z));

    // Line up on the axis, then close in along it
    double off_axis = std::hypot(x, z);
    double goal_y = off_axis < 0.5 ? 0 : std::max(y, 5.0);
    double ex = -x, ey = goal_y - y, ez = -z;
    double distance = std::sqrt(ex * ex + ey * ey + ez * ez);
    double speed = std::min(1.0, std::max(0.1, 0.2 * distance));
    double scale = speed / std::max(distance, 0.01);

    // Thrust towards the velocity we want, in the vessel's frame
    auto [vx, vy, vz] = velocity();
    auto thrust =
        std::make_tuple(2 * (ex * scale - vx), 2 * (ey * scale - vy),
                        2 * (ez * scale - vz));
    auto [right, forward, down] = space_center.transform_direction(
        thrust, frame, vessel.reference_frame());
    vessel.control().set_right(std::clamp(right, -1.0, 1.0));
    vessel.control().set_forward(std::clamp(forward, -1.0, 1.0));
    vessel.control().set_up(std::clamp(-down, -1.0, 1.0));
    std::this_thread::sleep_for(std::chrono::milliseconds(50));
  }

  // Stop the thrusters, and remove the drawing
  vessel.control().set_right(0);
  vessel.control().set_forward(0);
  vessel.control().set_up(0);
  vessel.auto_pilot().set_engaged(false);
  drawing.clear();
  std::cout << "Docked" << std::endl;
}
