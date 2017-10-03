#include <iostream>
#include <chrono>
#include <thread>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect("Sub-orbital flight");
  krpc::services::KRPC krpc(&conn);
  krpc::services::SpaceCenter space_center(&conn);

  auto vessel = space_center.active_vessel();

  vessel.auto_pilot().target_pitch_and_heading(90, 90);
  vessel.auto_pilot().engage();
  vessel.control().set_throttle(1);
  std::this_thread::sleep_for(std::chrono::seconds(1));

  std::cout << "Launch!" << std::endl;
  vessel.control().activate_next_stage();

  typedef krpc::services::KRPC::Expression Expr;

  {
    auto solid_fuel = vessel.resources().amount_call("SolidFuel");
    auto expr = Expr::less_than(
      conn, Expr::call(conn, solid_fuel), Expr::constant_float(conn, 0.1));
    auto event = krpc.add_event(expr);
    event.acquire();
    event.wait();
    event.release();
  }

  std::cout << "Booster separation" << std::endl;
  vessel.control().activate_next_stage();

  {
    auto mean_altitude = vessel.flight().mean_altitude_call();
    auto expr = Expr::greater_than(
      conn, Expr::call(conn, mean_altitude), Expr::constant_double(conn, 10000));
    auto event = krpc.add_event(expr);
    event.acquire();
    event.wait();
    event.release();
  }

  std::cout << "Gravity turn" << std::endl;
  vessel.auto_pilot().target_pitch_and_heading(60, 90);

  {
    auto apoapsis_altitude = vessel.orbit().apoapsis_altitude_call();
    auto expr = Expr::greater_than(
      conn, Expr::call(conn, apoapsis_altitude), Expr::constant_double(conn, 100000));
    auto event = krpc.add_event(expr);
    event.acquire();
    event.wait();
    event.release();
  }

  std::cout << "Launch stage separation" << std::endl;
  vessel.control().set_throttle(0);
  std::this_thread::sleep_for(std::chrono::seconds(1));
  vessel.control().activate_next_stage();
  vessel.auto_pilot().disengage();

  {
    auto srf_altitude = vessel.flight().surface_altitude_call();
    auto expr = Expr::less_than(
      conn, Expr::call(conn, srf_altitude), Expr::constant_double(conn, 1000));
    auto event = krpc.add_event(expr);
    event.acquire();
    event.wait();
    event.release();
  }

  vessel.control().activate_next_stage();

  while (vessel.flight(vessel.orbit().body().reference_frame()).vertical_speed() < -0.1) {
    std::cout << "Altitude = " << vessel.flight().surface_altitude() << " meters" << std::endl;
    std::this_thread::sleep_for(std::chrono::seconds(1));
  }
  std::cout << "Landed!" << std::endl;
}
