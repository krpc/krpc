#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  // [docs]
  auto ap = vessel.auto_pilot();
  // notch a known 1.4 Hz pitch and yaw bending mode from the start
  ap.set_pitch_yaw_rate_filter_mode(
      krpc::services::SpaceCenter::RateFilterMode::notch);
  ap.set_pitch_yaw_oscillation_frequency(1.4);
  // [/docs]
}
