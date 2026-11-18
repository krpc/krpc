#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  // [docs]
  auto apoapsis_ut = space_center.ut() + vessel.orbit().time_to_apoapsis();
  space_center.warp_to(apoapsis_ut - 10);
  // [/docs]
}
