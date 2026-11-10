#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <tuple>

using krpc::services::SpaceCenter;

int main() {
  auto conn = krpc::connect();
  SpaceCenter sc(conn);
  auto frame = sc.active_vessel().value().reference_frame();

  // Bind the static methods to the client once
  auto orbit = SpaceCenter::Orbit::bind(conn);
  for (auto v : {orbit.reference_plane_normal(frame),
                 orbit.reference_plane_direction(frame)}) {
    std::cout << std::get<0>(v) << ", " << std::get<1>(v) << ", "
              << std::get<2>(v) << std::endl;
  }
}
