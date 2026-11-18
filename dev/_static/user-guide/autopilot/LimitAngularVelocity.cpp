#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <tuple>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  // [docs]
  auto ap = vessel.auto_pilot();
  // limit the angular velocity by max_angular_velocity only
  ap.set_limit_angular_velocity(false);
  ap.set_max_angular_velocity(std::make_tuple(0.3, 0.5, 0.3));
  // [/docs]
}
