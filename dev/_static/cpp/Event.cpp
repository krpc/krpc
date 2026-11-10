#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(conn);
  auto flight = sc.active_vessel().value().flight();

  // Get the remote procedure call as a message object,
  // so it can be passed to the server
  auto mean_altitude = flight.mean_altitude_call();

  // Build the function on the server
  auto e = krpc::services::KRPC::Expression::bind(conn);
  auto expr = e.greater_than(e.call(mean_altitude), e.constant_double(1000));

  auto event = krpc::add_event(expr);
  event.acquire();
  event.wait();
  std::cout << "Altitude reached 1000m" << std::endl;
  event.release();
}
