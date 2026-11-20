#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  // [docs]
  auto ap = vessel.auto_pilot();
  ap.set_reference_frame(vessel.surface_reference_frame());
  ap.target_pitch_and_heading(90, 90);  // start pointing straight up
  ap.set_engaged(true);
  ap.wait();

  // ease a 10 degree pitch-over, 90 to 80, in over 20 seconds
  ap.set_target_smoothing_time(20);
  ap.target_pitch_and_heading(80, 90);
  ap.wait();  // returns once the 80 degree target is reached
  // [/docs]
}
