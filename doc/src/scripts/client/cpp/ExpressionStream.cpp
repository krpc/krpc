#include <chrono>
#include <iostream>
#include <thread>

#include <krpc.hpp>
#include <krpc/expression_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto flight = sc.active_vessel().flight();

  // Create an expression on the server, that computes
  // the vessel's altitude in kilometers
  typedef krpc::services::KRPC::Expression Expr;
  auto expr = Expr::divide(conn,
    Expr::call(conn, flight.mean_altitude_call()),
    Expr::constant_double(conn, 1000));

  // Stream the value of the expression
  auto stream = krpc::add_expression_stream<double>(expr);
  while (true) {
    std::cout << "Altitude: " << stream() << " km" << std::endl;
    std::this_thread::sleep_for(std::chrono::seconds(1));
  }
}
