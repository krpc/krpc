#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <map>
#include <string>
#include <vector>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  typedef krpc::services::KRPC::Expression Expr;
  typedef krpc::services::KRPC::Type KType;
  typedef krpc::services::SpaceCenter::Part Part;
  // [docs]
  auto e = Expr::bind(conn);
  auto t = KType::bind(conn);
  auto part = e.parameter("part", t.class_type("SpaceCenter", "Part"));
  // A property of the part parameter
  auto get = [&](const krpc::schema::ProcedureCall& property) {
    return e.call_with_arguments(property,
                                 std::map<int32_t, Expr>({{0, part}}));
  };
  // A function of the part parameter
  auto function = [&](const Expr& body) {
    return e.lambda(std::vector<Expr>({part}), body);
  };

  auto over =
      e.greater_than(e.add(get(Part::static_pressure_call_template()),
                           get(Part::dynamic_pressure_call_template())),
                     get(Part::max_pressure_call_template()));
  auto titles =
      e.select(e.where(e.call(vessel.parts().all_call()), function(over)),
               function(get(Part::title_call_template())));
  auto over_limit = krpc::run<std::vector<std::string>>(e.to_list(titles));
  // [/docs]
  for (const auto& title : over_limit) std::cout << title << std::endl;
}
