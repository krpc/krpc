#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/ksa.hpp>

int main() {
  krpc::Client conn = krpc::connect("Launch");
  krpc::services::KSA ksa(&conn);
  auto vehicle = ksa.launch_vehicle("Rocket", "CCSFS LC-39A");
  auto flight = vehicle.flight();

  vehicle.auto_pilot().target_pitch_and_heading(90, 90);
  vehicle.auto_pilot().engage();
  vehicle.control().set_throttle(1);
  vehicle.control().activate_next_sequence();
  vehicle.control().set_engine_on(true);
  std::cout << vehicle.name() << " has launched" << std::endl;

  typedef krpc::services::KRPC::Expression Expr;
  auto expr = Expr::greater_than(
    conn, Expr::call(conn, flight.mean_altitude_call()), Expr::constant_double(conn, 10000));
  auto event = krpc::add_event(expr);
  event.acquire();
  event.wait();
  event.release();

  vehicle.control().set_engine_on(false);
  vehicle.auto_pilot().disengage();
  std::cout << vehicle.name() << " reached 10 km" << std::endl;
}
