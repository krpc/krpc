#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <map>
#include <vector>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(conn);
  auto vessel = sc.active_vessel().value();
  typedef krpc::services::KRPC::Expression Expr;
  typedef krpc::services::KRPC::Type KType;
  typedef krpc::services::SpaceCenter::Parachute Parachute;
  // [docs]
  auto e = Expr::bind(conn);
  auto t = KType::bind(conn);

  // for (auto parachute : vessel.parts().parachutes())
  //   parachute.deploy();
  auto parachute =
      e.variable("parachute", t.class_type("SpaceCenter", "Parachute"));
  auto deploy =
      e.call_with_arguments(Parachute::deploy_call_template(),
                            std::map<int32_t, Expr>({{0, parachute}}));
  auto parachutes = e.call(vessel.parts().parachutes_call());
  auto program = e.block_with_variables(
      std::vector<Expr>({parachute}),
      std::vector<Expr>({e.for_each(parachute, parachutes, deploy)}));

  krpc::run(program);
  // [/docs]
}
