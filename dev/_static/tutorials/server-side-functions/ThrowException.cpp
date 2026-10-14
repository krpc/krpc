#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel().value();
  typedef krpc::services::KRPC::Expression Expr;
  // [docs]
  auto amount = vessel.resources().amount_call("LiquidFuel");
  auto expr = Expr::if_then(conn,
    Expr::less_than(conn,
      Expr::call(conn, amount), Expr::constant_double(conn, 100)),
    Expr::throw_(conn, "KRPC", "InvalidOperationException",
      Expr::constant_string(conn, "not enough fuel")));
  krpc::run(expr);
  // [/docs]
}
