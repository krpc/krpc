#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect("Launch");
  krpc::services::SpaceCenter space_center(&conn);
  auto vessel = space_center.active_vessel().value();
  auto flight = vessel.flight();

  vessel.auto_pilot().target_pitch_and_heading(90, 90);
  vessel.auto_pilot().set_engaged(true);
  vessel.control().set_throttle(1);
  vessel.control().activate_next_stage();
  std::cout << vessel.name() << " has launched" << std::endl;

  typedef krpc::services::KRPC::Expression Expr;
  auto expr = Expr::greater_than(
    conn, Expr::call(conn, flight.mean_altitude_call()), Expr::constant_double(conn, 10000));
  auto event = krpc::add_event(expr);
  event.acquire();
  event.wait();
  event.release();

  vessel.control().set_throttle(0);
  vessel.auto_pilot().set_engaged(false);
  std::cout << vessel.name() << " reached 10 km" << std::endl;
}
