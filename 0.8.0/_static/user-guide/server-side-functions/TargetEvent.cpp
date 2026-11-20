#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(conn);
  typedef krpc::services::KRPC::Expression Expr;
  auto some_vessel = sc.vessels()[0];
  // [docs]
  auto e = Expr::bind(conn);
  auto target = sc.target_vessel_call();
  auto expr = e.equal(e.call(target),
                      e.constant_object(krpc::get_object_id(some_vessel)));
  auto event = krpc::add_event(expr);
  // [/docs]
}
