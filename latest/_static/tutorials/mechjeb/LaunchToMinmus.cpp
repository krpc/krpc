#include <algorithm>
#include <cstdio>
#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/mech_jeb.hpp>
#include <krpc/services/space_center.hpp>
#include <memory>
#include <string>

using krpc::services::KRPC;
using krpc::services::MechJeb;
using krpc::services::SpaceCenter;

// Wait until the condition is true
void wait_for(const KRPC::Expression& condition) {
  auto event = krpc::add_event(condition);
  event.acquire();
  event.wait();
  event.release();
}

// Print each step of the autopilot, from a stream of its status
void print_steps(krpc::Stream<std::string> status) {
  auto last = std::make_shared<std::string>();
  status.add_callback([last](const std::string& value) {
    // The step, without the figures that change on each update
    std::string step = value.substr(0, value.find('\n'));
    step = step.substr(
        0, std::min(step.find_first_of("0123456789"), step.find("\u221e")));
    step.erase(step.find_last_not_of(" :=") + 1);
    if (!step.empty() && step != *last) {
      *last = step;
      std::cout << step << std::endl;
    }
  });
}

void print_orbit(SpaceCenter::Vessel vessel) {
  auto orbit = vessel.orbit();
  std::printf("Orbit of %s, %.0f km by %.0f km\n",
              orbit.body().name().c_str(), orbit.apoapsis_altitude() / 1000,
              orbit.periapsis_altitude() / 1000);
}

int main() {
  krpc::Connection conn = krpc::connect("Launch to Minmus");
  SpaceCenter sc(conn);
  MechJeb mj(conn);
  auto vessel = sc.active_vessel().value();
  auto e = KRPC::Expression::bind(conn);

  // The target orbit, set as in the Ascent Guidance window
  auto ascent = mj.ascent_guidance();
  ascent.set_ascent_type(MechJeb::AscentType::classic);
  ascent.set_desired_orbit_altitude(100000);
  ascent.set_desired_inclination(0);

  // The Ascent Settings window
  auto settings = ascent.settings();
  settings.set_autostage(true);
  settings.set_limit_angle_of_attack(true);
  settings.set_max_angle_of_attack(5);
  settings.set_autowarp(true);

  // Print each step of the autopilot
  auto status = ascent.status_stream();
  print_steps(status);

  ascent.set_enabled(true);
  vessel.control().activate_next_stage();

  // Wait for the autopilot to disengage, once the orbit is circular
  wait_for(e.not_(e.call(ascent.enabled_call())));
  status.remove();
  print_orbit(vessel);

  auto planner = mj.maneuver_planner();
  planner.set_autowarp(true);

  // Stage once a stage burns out, during the burns too
  mj.utilities().staging().set_enabled(true);

  // Execute the vessel's maneuver nodes, and wait for the burns
  auto execute_nodes = [&]() {
    planner.execute_all_nodes();
    wait_for(e.not_(e.call(planner.executing_node_call())));
  };

  // Target Minmus, and wait for MechJeb to see the target
  auto minmus = sc.bodies()["Minmus"];
  sc.set_target_body(minmus);
  auto target_controller = mj.target_controller();
  wait_for(e.call(target_controller.normal_target_exists_call()));

  // Match planes with Minmus, at the nearest relative node
  auto plane = planner.operation_plane();
  plane.time_selector().set_time_reference(
      MechJeb::TimeReference::rel_nearest_ad);
  plane.make_nodes();
  execute_nodes();

  // The Hohmann transfer, to an encounter with no capture burn
  auto transfer = planner.operation_transfer();
  transfer.time_selector().set_time_reference(
      MechJeb::TimeReference::computed);
  transfer.set_capture(false);
  transfer.make_nodes();
  execute_nodes();

  // Fine tune the encounter, for a periapsis 20 km above Minmus
  auto correction = planner.operation_course_correction();
  correction.set_final_periapsis_altitude(20000);
  correction.make_nodes();
  execute_nodes();

  // Warp into Minmus' sphere of influence
  sc.warp_to(sc.ut() + vessel.orbit().time_to_soi_change() + 60);

  // Circularize at the periapsis
  auto circularize = planner.operation_circularize();
  circularize.time_selector().set_time_reference(
      MechJeb::TimeReference::periapsis);
  circularize.make_nodes();
  execute_nodes();

  sc.clear_target();
  print_orbit(vessel);

  // Stage until the next stage would drop the landing legs. This drops
  // the stages below the lander, and activates the lander's engine.
  mj.utilities().staging().set_enabled(false);
  auto legs = vessel.parts().legs()[0].part();
  while (vessel.control().current_stage() > legs.decouple_stage() + 1)
    vessel.control().activate_next_stage();

  // Limit the lander's thrust, so its lowest throttle leaves it falling
  for (auto engine : vessel.parts().engines()) engine.set_thrust_limit(0.3);

  // Land on Minmus' Great Flats, and wait for MechJeb to see the target
  auto landing = mj.landing_guidance();
  landing.set_target(minmus, 0, -6);
  wait_for(e.call(target_controller.position_target_exists_call()));

  // The settings of the Landing Guidance window
  landing.set_touchdown_speed(1);
  landing.set_deploy_gears(true);
  landing.set_autowarp(true);

  // Print each step of the autopilot
  status = landing.status_stream();
  print_steps(status);

  landing.land_at_target();

  // Wait for the autopilot to disengage, once the vessel has landed
  wait_for(e.not_(e.call(landing.enabled_call())));
  status.remove();

  sc.clear_target();
  auto flight = vessel.flight();
  std::printf("Landed at latitude %.2f, longitude %.2f\n",
              flight.latitude(), flight.longitude());
}
