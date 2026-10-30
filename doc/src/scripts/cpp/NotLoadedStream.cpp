#include <optional>
#include <vector>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto flight = sc.active_vessel().value().flight();

  typedef krpc::services::KRPC::Expression Expr;
  typedef krpc::services::KRPC::Type KType;

  // [docs]
  // altitude = mean_altitude / 1000, or 0 if the vessel is not loaded
  auto altitude = Expr::variable(conn, "altitude", KType::double_(conn));
  auto altitude_km = Expr::block_with_variables(
    conn,
    std::vector<Expr>({altitude}),
    std::vector<Expr>({
      Expr::try_catch(conn,
        Expr::assign(conn, altitude,
          Expr::divide(conn,
            Expr::call(conn, flight.mean_altitude_call()),
            Expr::constant_double(conn, 1000))),
        "KRPC", "ObjectNotLoadedException", std::nullopt,
        Expr::assign(conn, altitude, Expr::constant_double(conn, 0))),
      altitude}));

  auto stream = krpc::add_stream<double>(altitude_km);
  // [/docs]
}
