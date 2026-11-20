#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

static float control_law(float pitch) { return 0.05f * (45 - pitch); }

int main() {
  auto conn = krpc::connect();
  krpc::services::KRPC krpc(conn);
  krpc::services::SpaceCenter sc(conn);
  auto vessel = sc.active_vessel().value();
  // [docs]
  while (true) {
    krpc.hold_tick();
    try {
      auto pitch = vessel.flight().pitch();
      vessel.control().set_pitch(control_law(pitch));
    } catch (...) {
      krpc.release_tick();
      throw;
    }
    krpc.release_tick();
  }
  // [/docs]
}
