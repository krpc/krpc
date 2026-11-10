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

  // Build a function that computes
  // the vessel's altitude in kilometers
  auto e = krpc::services::KRPC::Expression::bind(conn);
  auto expr =
      e.divide(e.call(flight.mean_altitude_call()), e.constant_double(1000));

  // Stream the value of the function
  auto stream = krpc::add_stream<double>(expr);
  while (true) {
    std::cout << "Altitude: " << stream() << " km" << std::endl;
    std::this_thread::sleep_for(std::chrono::seconds(1));
  }
}
