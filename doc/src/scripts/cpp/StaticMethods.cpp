#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <tuple>

using krpc::services::SpaceCenter;

int main() {
  auto conn = krpc::connect();
  SpaceCenter sc(conn);
  auto frame = sc.active_vessel().value().reference_frame();

  // Call a static method, passing the client first
  auto direction = SpaceCenter::Orbit::reference_plane_direction(conn, frame);
  std::cout << std::get<0>(direction) << ", " << std::get<1>(direction) << ", "
            << std::get<2>(direction) << std::endl;

  // Stream it
  auto stream =
      SpaceCenter::Orbit::reference_plane_direction_stream(conn, frame);
  direction = stream();
  std::cout << std::get<0>(direction) << ", " << std::get<1>(direction) << ", "
            << std::get<2>(direction) << std::endl;
}
