#include <iostream>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::KRPC krpc(&conn);
  krpc::services::SpaceCenter sc(&conn);
  auto flight = sc.active_vessel().flight();

  // Get the remote procedure call as a message object,
  // so it can be passed to the server
  auto mean_altitude = flight.mean_altitude_call();

  // Create an expression on the server
  typedef krpc::services::KRPC::Expression Expr;
  auto expr = Expr::greater_than(conn,
    Expr::call(conn, mean_altitude),
    Expr::constant_double(conn, 1000));

  auto event = krpc.add_event(expr);
  event.acquire();
  event.wait();
  std::cout << "Altitude reached 1000m" << std::endl;
  event.release();
}
