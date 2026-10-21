#include <iostream>
#include <chrono>
#include <cmath>
#include <thread>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect("Launch into orbit");
  krpc::services::SpaceCenter space_center(&conn);
  auto vessel = space_center.active_vessel().value();

  float turn_start_altitude = 250;
  float turn_end_altitude = 45000;
  float target_altitude = 150000;

  // Set up streams for telemetry
  auto ut = space_center.ut_stream();
  auto altitude = vessel.flight().mean_altitude_stream();
  auto apoapsis = vessel.orbit().apoapsis_altitude_stream();
  auto stage_2_resources = vessel.decouple_stage_at(2).resources(false);
  auto srb_fuel = stage_2_resources.amount_stream("SolidFuel");

  // Pre-launch setup
  vessel.control().set_sas(false);
  vessel.control().set_rcs(false);
  vessel.control().set_throttle(1);

  // Countdown...
  std::cout << "3..." << std::endl;
  std::this_thread::sleep_for(std::chrono::seconds(1));
  std::cout << "2..." << std::endl;
  std::this_thread::sleep_for(std::chrono::seconds(1));
  std::cout << "1..." << std::endl;
  std::this_thread::sleep_for(std::chrono::seconds(1));
  std::cout << "Launch!" << std::endl;

  // Activate the first stage
  vessel.control().activate_next_stage();
  vessel.auto_pilot().set_engaged(true);
  vessel.auto_pilot().target_pitch_and_heading(90, 90);

  // Main ascent loop
  bool srbs_separated = false;
  double turn_angle = 0;
  while (true) {
    // Gravity turn
    if (altitude() > turn_start_altitude && altitude() < turn_end_altitude) {
      double frac = (altitude() - turn_start_altitude)
                    / (turn_end_altitude - turn_start_altitude);
      double new_turn_angle = frac * 90.0;
      if (std::abs(new_turn_angle - turn_angle) > 0.5) {
        turn_angle = new_turn_angle;
        vessel.auto_pilot().target_pitch_and_heading(90.0 - turn_angle, 90.0);
      }
    }

    // Separate SRBs when finished
    if (!srbs_separated) {
      if (srb_fuel() < 0.1) {
        vessel.control().activate_next_stage();
        srbs_separated = true;
        std::cout << "SRBs separated" << std::endl;
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
  typedef krpc::services::KRPC::Expression Expr;
  {
    auto expr = Expr::greater_than_or_equal(
      conn, Expr::call(conn, vessel.orbit().apoapsis_altitude_call()),
      Expr::constant_double(conn, target_altitude));
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
    auto expr = Expr::greater_than_or_equal(
      conn, Expr::call(conn, vessel.flight().mean_altitude_call()),
      Expr::constant_double(conn, 70500));
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

  // Orientate ship
  std::cout << "Orientating ship for circularization burn" << std::endl;
  vessel.auto_pilot().set_reference_frame(node.reference_frame());
  vessel.auto_pilot().set_target_direction(std::make_tuple(0.0, 1.0, 0.0));
  vessel.auto_pilot().wait();

  // Wait until burn
  std::cout << "Waiting until circularization burn" << std::endl;
  double burn_ut = ut() + vessel.orbit().time_to_apoapsis() - (burn_time / 2.0);
  double lead_time = 5;
  space_center.warp_to(burn_ut - lead_time);

  // Execute burn
  std::cout << "Ready to execute burn" << std::endl;
  {
    auto expr = Expr::less_than_or_equal(
      conn, Expr::call(conn, vessel.orbit().time_to_apoapsis_call()),
      Expr::constant_double(conn, burn_time / 2.0));
    auto event = krpc::add_event(expr);
    event.acquire();
    event.wait();
    event.release();
  }
  std::cout << "Executing burn" << std::endl;
  vessel.control().set_throttle(1);
  std::this_thread::sleep_for(
    std::chrono::milliseconds(static_cast<int>((burn_time - 0.1) * 1000)));
  std::cout << "Fine tuning" << std::endl;
  vessel.control().set_throttle(0.05);
  {
    auto remaining_burn = Expr::call(
      conn, node.remaining_burn_vector_call(node.reference_frame()));
    auto expr = Expr::less_than_or_equal(
      conn, Expr::get(conn, remaining_burn, Expr::constant_int(conn, 1)),
      Expr::constant_double(conn, 0));
    auto event = krpc::add_event(expr);
    event.acquire();
    event.wait();
    event.release();
  }
  vessel.control().set_throttle(0);
  node.remove();

  std::cout << "Launch complete" << std::endl;
}
