#include <iostream>
#include <map>
#include <vector>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::KRPC krpc(&conn);
  krpc::services::SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel().value();

  typedef krpc::services::KRPC::Expression Expr;
  typedef krpc::services::KRPC::Type KType;

  // A function taking an engine, returning true if it is out of fuel
  auto engine = Expr::parameter(conn, "engine",
    KType::class_type(conn, "SpaceCenter", "Engine"));
  auto has_fuel = vessel.parts().engines()[0].has_fuel_call();
  auto out_of_fuel = Expr::lambda(conn,
    std::vector<Expr>({engine}),
    Expr::not_(conn, Expr::call_with_arguments(
      conn, has_fuel, std::map<int32_t, Expr>({{0, engine}}))));

  // Whether any engine satisfies it
  auto engines = vessel.parts().engines_call();
  auto expr = Expr::any(conn, Expr::call(conn, engines), out_of_fuel);

  auto event = krpc.add_event(expr);
  event.acquire();
  event.wait();
  std::cout << "An engine has run out of fuel" << std::endl;
  event.release();
}
