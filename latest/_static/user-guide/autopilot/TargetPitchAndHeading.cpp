#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  // [docs]
  auto ap = vessel.auto_pilot();
  ap.set_reference_frame(vessel.surface_reference_frame());
  // 80 degrees above the horizon, facing east
  ap.target_pitch_and_heading(80, 90);
  ap.set_engaged(true);
  ap.wait();  // blocks until the vessel has settled on the target
  ap.set_engaged(false);
  // [/docs]
}
