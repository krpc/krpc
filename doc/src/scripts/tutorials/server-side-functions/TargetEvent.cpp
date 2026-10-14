#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  typedef krpc::services::KRPC::Expression Expr;
  auto some_vessel = sc.vessels()[0];
  // [docs]
  auto target = sc.target_vessel_call();
  auto expr = Expr::equal(conn,
    Expr::call(conn, target),
    Expr::constant_object(conn, some_vessel._id));
  auto event = krpc::add_event(expr);
  // [/docs]
}
