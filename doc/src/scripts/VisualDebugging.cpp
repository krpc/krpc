#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/ui.hpp>
#include <krpc/services/drawing.hpp>

int main() {
  krpc::Client conn = krpc::connect("Visual Debugging");
  krpc::services::SpaceCenter space_center(&conn);
  krpc::services::Drawing drawing(&conn);
  auto vessel = space_center.active_vessel();

  auto ref_frame = vessel.surface_velocity_reference_frame();
  drawing.add_direction(std::make_tuple(0, 1, 0), ref_frame);
  while (true) {
  }
}
