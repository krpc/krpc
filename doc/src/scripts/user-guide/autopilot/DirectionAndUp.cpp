#include <cmath>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <tuple>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  const double pi = std::acos(-1.0);
  // [docs]
  auto ap = vessel.auto_pilot();
  ap.set_reference_frame(vessel.surface_reference_frame());
  // north, in the surface frame, off the flight path
  auto up = std::make_tuple(0.0, 1.0, 0.0);

  // straight up at the pad, roof toward north
  ap.set_direction_and_up(std::make_tuple(1.0, 0.0, 0.0), up, 0);
  ap.set_engaged(true);
  ap.wait();

  // ease over to the eastern horizon; roll stays defined through vertical
  for (int i = 1; i <= 90; i++) {
    double pitch = (90 - i) * pi / 180;  // 89 down to 0 degrees
    auto direction = std::make_tuple(std::sin(pitch), 0.0, std::cos(pitch));
    ap.set_direction_and_up(direction, up, 0);
    ap.wait();
  }
  // [/docs]
}
