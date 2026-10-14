#include <iostream>
#include <tuple>

#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel().value();
  auto flight = vessel.flight(vessel.orbit().body().reference_frame());

  // Get the remote procedure calls as message objects,
  // so they can be passed to the server
  auto mean_altitude_call = flight.mean_altitude_call();
  auto speed_call = flight.speed_call();

  // Build a function that reads the altitude and the speed
  typedef krpc::services::KRPC::Expression Expr;
  auto expr = Expr::create_tuple(conn, {
    Expr::call(conn, mean_altitude_call),
    Expr::call(conn, speed_call)});

  // Run the function once, so both values come from the same physics tick
  auto [altitude, speed] = krpc::run<std::tuple<double, double>>(expr);
  std::cout << "Altitude: " << altitude << " m, speed: " << speed << " m/s" << std::endl;
}
