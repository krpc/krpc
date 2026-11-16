#include <algorithm>
#include <chrono>
#include <cmath>
#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <thread>

int main() {
  krpc::Connection conn = krpc::connect("Launch into orbit");
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();

  float turn_start_altitude = 250;
  float turn_end_altitude = 45000;
  float target_altitude = 150000;

  // Set up streams for telemetry
  auto ut = space_center.ut_stream();
  auto altitude = vessel.flight().mean_altitude_stream();
  auto apoapsis = vessel.orbit().apoapsis_altitude_stream();
  auto stage_5_resources = vessel.decouple_stage_at(5).resources(false);
  auto stage_3_resources = vessel.decouple_stage_at(3).resources(false);
  auto first_stage_fuel = stage_5_resources.amount_stream("SolidFuel");
  auto second_stage_fuel = stage_3_resources.amount_stream("LiquidFuel");

  // Pre-launch setup
  vessel.control().set_sas(false);
  vessel.control().set_rcs(false);
  vessel.control().set_throttle(1);

  // Activate the first stage
  vessel.auto_pilot().set_engaged(true);
  vessel.auto_pilot().target_pitch_and_heading(90, 90);
  vessel.control().activate_next_stage();

  // Main ascent loop
  bool first_stage_separated = false;
  bool second_stage_separated = false;
  while (true) {
    // Gravity turn
    if (altitude() > turn_start_altitude &&
        altitude() < turn_end_altitude) {
      double frac = (altitude() - turn_start_altitude) /
                    (turn_end_altitude - turn_start_altitude);
      double turn_angle = frac * 90.0;
      vessel.auto_pilot().target_pitch_and_heading(90.0 - turn_angle, 90.0);
    }

    if (!first_stage_separated) {
      if (first_stage_fuel() < 0.1) {
        vessel.control().activate_next_stage();
        vessel.control().activate_next_stage();
        first_stage_separated = true;
        std::cout << "First stage separated" << std::endl;
      }
    } else if (!second_stage_separated) {
      if (second_stage_fuel() < 0.1) {
        vessel.control().activate_next_stage();
        vessel.control().activate_next_stage();
        second_stage_separated = true;
        std::cout << "Second stage separated" << std::endl;
      }
    }

    // Decrease throttle when approaching target apoapsis
    if (apoapsis() > target_altitude * 0.9) {
      std::cout << "Approaching target apoapsis" << std::endl;
      break;
    }
  }

  // Disable engines when target apoapsis is reached
  vessel.control().set_throttle(0.25);
  auto e = krpc::services::KRPC::Expression::bind(conn);
  {
    auto expr = e.greater_than_or_equal(
        e.call(vessel.orbit().apoapsis_altitude_call()),
        e.constant_double(target_altitude));
    auto event = krpc::add_event(expr);
    event.acquire();
    event.wait();
    event.release();
  }
  std::cout << "Target apoapsis reached" << std::endl;
  vessel.control().set_throttle(0);

  // Wait until out of atmosphere
  std::cout << "Coasting out of atmosphere" << std::endl;
  {
    auto expr = e.greater_than_or_equal(
        e.call(vessel.flight().mean_altitude_call()),
        e.constant_double(70500));
    auto event = krpc::add_event(expr);
    event.acquire();
    event.wait();
    event.release();
  }

  // Plan circularization burn (using vis-viva equation)
  std::cout << "Planning circularization burn" << std::endl;
  double mu = vessel.orbit().body().gravitational_parameter();
  double r = vessel.orbit().apoapsis();
  double a1 = vessel.orbit().semi_major_axis();
  double a2 = r;
  double v1 = std::sqrt(mu * ((2.0 / r) - (1.0 / a1)));
  double v2 = std::sqrt(mu * ((2.0 / r) - (1.0 / a2)));
  double delta_v = v2 - v1;
  auto node = vessel.control().add_node(
      ut() + vessel.orbit().time_to_apoapsis(), delta_v);

  // Calculate burn time (using rocket equation)
  double F = vessel.available_thrust();
  double Isp = vessel.specific_impulse() * 9.82;
  double m0 = vessel.mass();
  double m1 = m0 / std::exp(delta_v / Isp);
  double flow_rate = F / Isp;
  double burn_time = (m0 - m1) / flow_rate;

  // Orientate ship along the burn
  std::cout << "Orientating ship for circularization burn" << std::endl;
  auto frame = vessel.orbit().body().non_rotating_reference_frame();
  vessel.auto_pilot().set_reference_frame(frame);
  vessel.auto_pilot().set_target_direction(
      node.remaining_burn_vector(frame));
  vessel.auto_pilot().wait();

  // Wait until burn
  std::cout << "Waiting until circularization burn" << std::endl;
  double burn_ut =
      ut() + vessel.orbit().time_to_apoapsis() - (burn_time / 2.0);
  double lead_time = 5;
  space_center.warp_to(burn_ut - lead_time);

  // Execute burn
  std::cout << "Ready to execute burn" << std::endl;
  {
    auto expr =
        e.less_than_or_equal(e.call(vessel.orbit().time_to_apoapsis_call()),
                             e.constant_double(burn_time / 2.0));
    auto event = krpc::add_event(expr);
    event.acquire();
    event.wait();
    event.release();
  }
  std::cout << "Executing burn" << std::endl;
  auto remaining_burn = node.remaining_burn_vector_stream(frame);
  auto remaining_delta_v = node.remaining_delta_v_stream();
  double lowest_delta_v = remaining_delta_v();
  while (remaining_delta_v() > 0.1) {
    // Steer along the remaining burn, which turns as the vessel moves
    // around its orbit
    vessel.auto_pilot().set_target_direction(remaining_burn());
    // Full throttle, then throttle down for the last few m/s
    vessel.control().set_throttle(remaining_delta_v() > 5 ? 1.0 : 0.05);
    // Stop if the burn overshoots, as the remaining delta-v then grows
    if (remaining_delta_v() > lowest_delta_v + 0.1) break;
    lowest_delta_v = std::min(lowest_delta_v, remaining_delta_v());
    std::this_thread::sleep_for(std::chrono::milliseconds(50));
  }
  vessel.control().set_throttle(0);
  node.remove();

  std::cout << "Launch complete" << std::endl;
}
