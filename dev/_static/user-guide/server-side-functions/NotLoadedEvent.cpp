#include <iostream>
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

  // above = mean_altitude > 1000, or false if the vessel is not loaded
  auto above = Expr::variable(conn, "above", KType::bool_(conn));
  auto above_1000m = Expr::block_with_variables(
    conn,
    std::vector<Expr>({above}),
    std::vector<Expr>({
      Expr::try_catch(conn,
        Expr::assign(conn, above,
          Expr::greater_than(conn,
            Expr::call(conn, flight.mean_altitude_call()),
            Expr::constant_double(conn, 1000))),
        "KRPC", "ObjectNotLoadedException", std::nullopt,
        Expr::assign(conn, above, Expr::constant_bool(conn, false))),
      above}));

  auto event = krpc::add_event(above_1000m);
  event.acquire();
  event.wait();
  std::cout << "Altitude reached 1000m" << std::endl;
  event.release();
}
