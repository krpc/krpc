#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/std_lib.hpp>
#include <map>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(conn);
  auto vessel = sc.active_vessel().value();
  auto flight = vessel.flight();
  auto control = vessel.control();
  double target_pitch = 45;
  typedef krpc::services::KRPC::Expression Expr;
  typedef krpc::services::KRPC::Type KType;
  typedef krpc::services::SpaceCenter::Control Control;
  typedef krpc::services::StdLib StdLib;
  // [docs]
  auto e = Expr::bind(conn);
  auto t = KType::bind(conn);

  // control.set_pitch(
  //   std::clamp(0.05 * (target_pitch - flight.pitch()), -1.0, 1.0));
  auto error = e.subtract(e.constant_double(target_pitch),
                          e.call(flight.pitch_call()));
  auto command = e.call_with_arguments(
      StdLib::clamp_call_template(),
      std::map<int32_t, Expr>(
          {{0, e.multiply(e.constant_double(0.05), error)},
           {1, e.constant_double(-1)},
           {2, e.constant_double(1)}}));
  auto step = krpc::compile(e.call_with_arguments(
      Control::set_pitch_call_template(),
      std::map<int32_t, Expr>(
          {{0, e.constant_object(krpc::get_object_id(control))},
           {1, e.cast(command, t.float_())}})));

  while (true) krpc::run(step);
  // [/docs]
}
