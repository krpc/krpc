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

  // [docs]
  auto e = Expr::bind(conn);
  auto t = KType::bind(conn);
  // altitude = mean_altitude / 1000, or 0 if the vessel is not loaded
  auto altitude = e.variable("altitude", t.double_());
  auto km =
      e.divide(e.call(flight.mean_altitude_call()), e.constant_double(1000));
  auto altitude_km = e.block_with_variables(
      std::vector<Expr>({altitude}),
      std::vector<Expr>({e.try_catch(e.assign(altitude, km), "KRPC",
                                     "ObjectNotLoadedException", std::nullopt,
                                     e.assign(altitude, e.constant_double(0))),
                         altitude}));

  auto stream = krpc::add_stream<double>(altitude_km);
  // [/docs]
}
