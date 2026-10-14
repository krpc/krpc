#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel().value();
  auto flight = vessel.flight();
  typedef krpc::services::KRPC::Expression Expr;
  typedef krpc::services::KRPC::Type KType;
  auto mean_altitude = flight.mean_altitude_call();
  // [docs]
  // Truncate the altitude to a whole number of meters
  auto expr = Expr::cast(conn,
    Expr::call(conn, mean_altitude),
    KType::int_(conn));
  // [/docs]
}
