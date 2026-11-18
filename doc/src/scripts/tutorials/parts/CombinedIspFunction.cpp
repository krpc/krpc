#include <iomanip>
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
  typedef krpc::services::SpaceCenter::Engine Engine;
  // [docs]
  auto e = Expr::bind(conn);
  auto t = KType::bind(conn);
  auto engine =
      e.parameter("engine", t.class_type("SpaceCenter", "Engine"));
  // A property of the engine parameter
  auto get = [&](const krpc::schema::ProcedureCall& property) {
    return e.call_with_arguments(property,
                                 std::map<int32_t, Expr>({{0, engine}}));
  };
  // A function of the engine parameter
  auto function = [&](const Expr& body) {
    return e.lambda(std::vector<Expr>({engine}), body);
  };

  // The active engines with fuel
  auto engines = e.where(
      e.call(vessel.parts().engines_call()),
      function(e.conditional_and(get(Engine::active_call_template()),
                                 get(Engine::has_fuel_call_template()))));
  auto thrust = get(Engine::available_thrust_call_template());
  auto isp = get(Engine::specific_impulse_call_template());
  auto total_thrust = e.sum(e.select(engines, function(thrust)));
  auto fuel_consumption =
      e.sum(e.select(engines, function(e.divide(thrust, isp))));

  auto combined_isp =
      krpc::run<float>(e.divide(total_thrust, fuel_consumption));
  // [/docs]
  std::cout << "Combined Isp = " << std::fixed << std::setprecision(0)
            << combined_isp << " seconds" << std::endl;
}
