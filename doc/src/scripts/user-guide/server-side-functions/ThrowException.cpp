#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(conn);
  auto vessel = sc.active_vessel().value();
  typedef krpc::services::KRPC::Expression Expr;
  // [docs]
  auto e = Expr::bind(conn);
  auto amount = vessel.resources().amount_call("LiquidFuel");
  auto expr = e.if_then(e.less_than(e.call(amount), e.constant_double(100)),
                        e.throw_("KRPC", "InvalidOperationException",
                                 e.constant_string("not enough fuel")));
  krpc::run(expr);
  // [/docs]
}
