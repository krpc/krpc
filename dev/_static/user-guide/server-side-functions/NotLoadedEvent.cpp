#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <optional>
#include <vector>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(conn);
  auto flight = sc.active_vessel().value().flight();

  typedef krpc::services::KRPC::Expression Expr;
  typedef krpc::services::KRPC::Type KType;
  auto e = Expr::bind(conn);
  auto t = KType::bind(conn);

  // above = mean_altitude > 1000, or false if the vessel is not loaded
  auto above = e.variable("above", t.bool_());
  auto altitude = e.call(flight.mean_altitude_call());
  auto above_1000m = e.block_with_variables(
      std::vector<Expr>({above}),
      std::vector<Expr>(
          {e.try_catch(e.assign(above, e.greater_than(altitude,
                                                      e.constant_double(1000))),
                       "KRPC", "ObjectNotLoadedException", std::nullopt,
                       e.assign(above, e.constant_bool(false))),
           above}));

  auto event = krpc::add_event(above_1000m);
  event.acquire();
  event.wait();
  std::cout << "Altitude reached 1000m" << std::endl;
  event.release();
}
