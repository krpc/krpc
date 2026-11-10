#include <iostream>
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
  auto e = Expr::bind(conn);
  auto t = KType::bind(conn);

  // A function taking an engine, returning true if it is out of fuel
  auto engine = e.parameter("engine", t.class_type("SpaceCenter", "Engine"));
  auto has_fuel = vessel.parts().engines()[0].has_fuel_call();
  auto out_of_fuel =
      e.lambda(std::vector<Expr>({engine}),
               e.not_(e.call_with_arguments(
                   has_fuel, std::map<int32_t, Expr>({{0, engine}}))));

  // Whether any engine satisfies it
  auto engines = vessel.parts().engines_call();
  auto expr = e.any(e.call(engines), out_of_fuel);

  auto event = krpc::add_event(expr);
  event.acquire();
  event.wait();
  std::cout << "An engine has run out of fuel" << std::endl;
  event.release();
}
