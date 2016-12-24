#include <iostream>
#include <vector>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

using SpaceCenter = krpc::services::SpaceCenter;

int main() {
  auto conn = krpc::connect();
  SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel();

  auto engines = vessel.parts().engines();

  std::vector<SpaceCenter::Engine> active_engines;
  for (auto engine : engines)
    if (engine.active() && engine.has_fuel())
      active_engines.push_back(engine);

  std::cout << "Active engines:" << std::endl;
  for (auto engine : active_engines)
    std::cout << "   " << engine.part().title() << " in stage " << engine.part().stage() << std::endl;

  double thrust = 0;
  double fuel_consumption = 0;
  for (auto engine : active_engines) {
    thrust += engine.thrust();
    fuel_consumption += engine.thrust() / engine.specific_impulse();
  }
  double isp = thrust / fuel_consumption;
  std::cout << "Combined vacuum Isp = " << isp << " seconds" << std::endl;
}
