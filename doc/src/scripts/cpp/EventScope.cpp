#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(conn);
  auto flight = sc.active_vessel().value().flight();
  auto mean_altitude = flight.mean_altitude_call();
  typedef krpc::services::KRPC::Expression Expr;
  auto expr = Expr::greater_than(conn, Expr::call(conn, mean_altitude),
                                 Expr::constant_double(conn, 1000));
  {
    auto event = krpc::add_event(expr);
    event.get_condition_lock().lock();
    event.wait();
    std::cout << "Altitude reached 1000m" << std::endl;
  }
  // The end of the block released the lock and removed the event
}
