#include <iostream>
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
  krpc::Stream<std::tuple<double, double, double>> pos_stream = vessel.position_stream(ref_frame);
  while (true) {
    std::tuple<double, double, double> pos = pos_stream();
    std::cout << std::get<0>(pos) << ","
              << std::get<1>(pos) << ","
              << std::get<2>(pos) << std::endl;
  }
}
