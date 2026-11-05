#include <cmath>
#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <tuple>

static const double pi = 3.1415926535897;

int main() {
  krpc::Client conn = krpc::connect("Raycast Example");
  krpc::services::SpaceCenter sc(&conn);
  auto part = sc.active_vessel().value().parts().root();
  auto frame = part.reference_frame();

  // A fan of 8 rays below the part, 10 degrees apart
  for (int i = 0; i < 8; i++) {
    double angle = (10 * i - 35) * pi / 180;
    auto direction = std::make_tuple(std::sin(angle), -std::cos(angle), 0.0);
    std::cout << sc.raycast_distance({0, 0, 0}, direction, frame) << std::endl;
  }
}
