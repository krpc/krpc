#include <chrono>
#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <thread>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(conn);
  auto flight = sc.active_vessel().value().flight();

  auto e = krpc::services::KRPC::Expression::bind(conn);
  auto expr = e.greater_than(e.call(flight.mean_altitude_call()),
                             e.constant_double(1000));
  auto event = krpc::add_event(expr);

  event.add_callback(
      [] { std::cout << "Altitude reached 1000m" << std::endl; });

  // Keep the program running, while the callback is called
  while (true) std::this_thread::sleep_for(std::chrono::seconds(1));
}
