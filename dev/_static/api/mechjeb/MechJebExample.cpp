#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/mech_jeb.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect("MechJeb Example");
  krpc::services::SpaceCenter sc(conn);
  krpc::services::MechJeb mj(conn);
  auto vessel = sc.active_vessel().value();
  auto ascent = mj.ascent_autopilot();

  ascent.set_desired_orbit_altitude(100000);
  ascent.set_desired_inclination(6);
  ascent.set_autostage(true);
  ascent.set_enabled(true);

  vessel.control().activate_next_stage();

  auto e = krpc::services::KRPC::Expression::bind(conn);
  auto expr = e.equal(e.call(ascent.enabled_call()), e.constant_bool(false));
  auto event = krpc::add_event(expr);
  event.acquire();
  event.wait();
  event.release();

  std::cout << "Launch complete" << std::endl;
}
