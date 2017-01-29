#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/ui.hpp>
#include <krpc/services/drawing.hpp>

int main() {
  krpc::Client conn = krpc::connect("Visual Debugging");
  krpc::services::SpaceCenter space_center(&conn);
  krpc::services::Drawing drawing(&conn);
  auto vessel = space_center.active_vessel();

  auto ref_frame = vessel.orbit().body().reference_frame();
  auto velocity = vessel.flight(ref_frame).velocity();
  drawing.add_direction(velocity, ref_frame);

  while (true) {
  }
}
