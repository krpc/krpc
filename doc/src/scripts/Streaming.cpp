#include <iostream>
#include <iomanip>
#include <tuple>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

using SpaceCenter = krpc::services::SpaceCenter;

int main() {
  krpc::Client conn = krpc::connect();
  krpc::services::KRPC krpc(&conn);
  SpaceCenter sc(&conn);
  SpaceCenter::Vessel vessel = sc.active_vessel();
  SpaceCenter::ReferenceFrame ref_frame = vessel.orbit().body().reference_frame();
  while (true) {
    std::tuple<double, double, double> pos = vessel.position(ref_frame);
    std::cout << std::fixed << std::setprecision(1);
    std::cout << std::get<0>(pos) << ", "
              << std::get<1>(pos) << ", "
              << std::get<2>(pos) << std::endl;
  }
}
