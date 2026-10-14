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
  auto site_info = vessel.comms().control_path()[0].start_call();
  // [docs]
  // The Name field of a structure a call returns
  auto expr = Expr::get_field(conn, Expr::call(conn, site_info), "Name");
  // [/docs]
}
