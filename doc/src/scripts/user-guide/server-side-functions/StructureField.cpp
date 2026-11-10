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
  auto site_info = vessel.comms().control_path()[0].start_call();
  // [docs]
  auto e = Expr::bind(conn);
  // The Name field of a structure a call returns
  auto expr = e.get_field(e.call(site_info), "Name");
  // [/docs]
}
