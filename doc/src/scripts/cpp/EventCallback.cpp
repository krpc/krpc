#include <chrono>
#include <iostream>
#include <thread>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto flight = sc.active_vessel().value().flight();

  typedef krpc::services::KRPC::Expression Expr;
  auto expr = Expr::greater_than(conn,
    Expr::call(conn, flight.mean_altitude_call()),
    Expr::constant_double(conn, 1000));
  auto event = krpc::add_event(expr);

  event.add_callback([] {
    std::cout << "Altitude reached 1000m" << std::endl;
  });

  // Keep the program running, while the callback is called
  while (true)
    std::this_thread::sleep_for(std::chrono::seconds(1));
}
