#include <iostream>
#include <iomanip>
#include <tuple>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel();
  auto ref_frame = vessel.orbit().body().reference_frame();
  auto pos_stream = vessel.position_stream(ref_frame);
  while (true) {
    auto pos = pos_stream();
    std::cout << std::fixed << std::setprecision(1);
    std::cout << std::get<0>(pos) << ", "
              << std::get<1>(pos) << ", "
              << std::get<2>(pos) << std::endl;
  }
}
