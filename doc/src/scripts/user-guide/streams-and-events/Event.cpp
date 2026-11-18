#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto flight = space_center.active_vessel().value().flight();
  // [docs]
  auto e = krpc::services::KRPC::Expression::bind(conn);
  auto event = krpc::add_event(e.greater_than(
      e.call(flight.mean_altitude_call()), e.constant_double(10000)));
  event.acquire();
  event.wait();
  event.release();
  std::cout << "Above 10 km" << std::endl;
  // [/docs]
}
