#include <chrono>
#include <iomanip>
#include <iostream>
#include <krpc.hpp>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <thread>

int main() {
  krpc::Connection conn = krpc::connect("Sub-orbital flight");
  krpc::services::SpaceCenter space_center(conn);

  auto vessel = space_center.active_vessel().value();

  vessel.auto_pilot().target_pitch_and_heading(90, 90);
  vessel.auto_pilot().set_engaged(true);
  vessel.control().set_throttle(0.5);
  std::this_thread::sleep_for(std::chrono::seconds(1));

  std::cout << "Launch!" << std::endl;
  vessel.control().activate_next_stage();

  auto e = krpc::services::KRPC::Expression::bind(conn);

  auto flight = vessel.flight();
  {
    auto expr = e.greater_than(e.call(flight.mean_altitude_call()),
                               e.constant_double(500));
    auto event = krpc::add_event(expr);
    event.acquire();
    event.wait();
    event.release();
  }

  std::cout << "Pitching over" << std::endl;
  vessel.auto_pilot().set_target_smoothing_time(10);
  vessel.auto_pilot().target_pitch_and_heading(70, 90);

  {
    auto liquid_fuel = vessel.resources().amount_call("LiquidFuel");
    auto expr = e.less_than(e.call(liquid_fuel), e.constant_float(0.1));
    auto event = krpc::add_event(expr);
    event.acquire();
    event.wait();
    event.release();
  }

  std::cout << "Flame out" << std::endl;
  vessel.control().set_throttle(0);
  vessel.auto_pilot().set_reference_frame(
      vessel.surface_velocity_reference_frame());
  vessel.auto_pilot().set_target_direction(std::make_tuple(0.0, 1.0, 0.0));

  auto body_flight = vessel.flight(vessel.orbit().body().reference_frame());
  {
    auto expr = e.less_than(e.call(body_flight.vertical_speed_call()),
                            e.constant_double(0));
    auto event = krpc::add_event(expr);
    event.acquire();
    event.wait();
    event.release();
  }

  std::cout << "Apoapsis reached, separating" << std::endl;
  vessel.control().activate_next_stage();
  vessel.auto_pilot().set_target_direction(std::make_tuple(0.0, -1.0, 0.0));
  vessel.auto_pilot().wait();
  vessel.auto_pilot().set_engaged(false);

  {
    auto expr = e.less_than(e.call(flight.surface_altitude_call()),
                            e.constant_double(2000));
    auto event = krpc::add_event(expr);
    event.acquire();
    event.wait();
    event.release();
  }

  std::cout << "Deploying the parachute" << std::endl;
  vessel.parts().parachutes()[0].set_deploy_altitude(550);
  vessel.control().activate_next_stage();

  using krpc::services::SpaceCenter;
  while (vessel.situation() != SpaceCenter::VesselSituation::landed &&
         vessel.situation() != SpaceCenter::VesselSituation::splashed) {
    std::cout << "Altitude = " << std::fixed << std::setprecision(1)
              << flight.surface_altitude() << " meters" << std::endl;
    std::this_thread::sleep_for(std::chrono::seconds(1));
  }
  std::cout << "Landed!" << std::endl;
}
