#include <iostream>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

static float control_law(float pitch) {
  return pitch + 0.05f * (45 - pitch);
}

static void record(float error, float pitch) {
  std::cout << error << " " << pitch << std::endl;
}

int main() {
  auto conn = krpc::connect();
  krpc::services::KRPC krpc(conn);
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  auto ap = vessel.auto_pilot();
  ap.set_engaged(true);
  // [docs]
  ap.set_update_mode(
      krpc::services::SpaceCenter::AutoPilotUpdateMode::manual);
  while (true) {
    krpc.hold_tick();
    try {
      ap.target_pitch_and_heading(control_law(vessel.flight().pitch()), 90);
      ap.update();
      record(ap.error(), vessel.control().pitch());
    } catch (...) {
      krpc.release_tick();
      throw;
    }
    krpc.release_tick();
  }
  // [/docs]
}
