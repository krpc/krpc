#include <iomanip>
#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <vector>

using SpaceCenter = krpc::services::SpaceCenter;

int main() {
  auto conn = krpc::connect();
  SpaceCenter sc(conn);
  auto vessel = sc.active_vessel().value();

  auto engines = vessel.parts().engines();

  std::vector<SpaceCenter::Engine> active_engines;
  for (auto engine : engines)
    if (engine.active() && engine.has_fuel())
      active_engines.push_back(engine);

  std::cout << "Active engines:" << std::endl;
  for (auto engine : active_engines)
    std::cout << "   " << engine.part().title() << " in stage "
              << engine.part().stage() << std::endl;

  double thrust = 0;
  double fuel_consumption = 0;
  for (auto engine : active_engines) {
    thrust += engine.available_thrust();
    fuel_consumption +=
        engine.available_thrust() / engine.specific_impulse();
  }
  double isp = thrust / fuel_consumption;
  std::cout << "Combined Isp = " << std::fixed << std::setprecision(0)
            << isp << " seconds" << std::endl;
}
