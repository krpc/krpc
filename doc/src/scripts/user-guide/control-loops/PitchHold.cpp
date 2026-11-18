// [docs]
#include <algorithm>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect("pitch hold");
  krpc::services::KRPC krpc(conn);
  krpc::services::SpaceCenter sc(conn);
  auto vessel = sc.active_vessel().value();
  float target_pitch = 45;

  try {
    for (int i = 0; i < 500; i++) {
      krpc.next_tick();
      auto flight = vessel.flight();
      auto error = target_pitch - flight.pitch();
      vessel.control().set_pitch(
          std::max(-1.0f, std::min(1.0f, 0.05f * error)));
    }
  } catch (...) {
    krpc.release_tick();
    vessel.control().set_pitch(0);
    throw;
  }
  krpc.release_tick();
  vessel.control().set_pitch(0);
}
// [/docs]
