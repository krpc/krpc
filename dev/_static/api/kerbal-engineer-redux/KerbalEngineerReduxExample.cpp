#include <iomanip>
#include <iostream>
#include <krpc.hpp>
#include <krpc/services/kerbal_engineer_redux.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Connection conn = krpc::connect("Kerbal Engineer Redux Example");
  krpc::services::SpaceCenter sc(conn);
  krpc::services::KerbalEngineerRedux ker(conn);
  auto vessel = sc.active_vessel().value();

  auto stages = ker.stages(vessel);
  for (auto stage = stages.rbegin(); stage != stages.rend(); stage++) {
    std::cout << std::fixed << "Stage " << stage->number << ": "
              << std::setprecision(0) << stage->delta_v << " m/s, TWR "
              << std::setprecision(2) << stage->available_twr << std::endl;
  }

  auto burn = ker.burn_time(vessel, 500);
  if (!burn)
    std::cout << "Not enough delta-v for the burn" << std::endl;
  else
    std::cout << "A 500 m/s burn takes " << std::setprecision(1)
              << burn->duration << " seconds" << std::endl;
}
