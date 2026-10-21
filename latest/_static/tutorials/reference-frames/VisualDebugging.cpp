#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/ui.hpp>
#include <krpc/services/drawing.hpp>

int main() {
  krpc::Client conn = krpc::connect("Visual Debugging");
  krpc::services::SpaceCenter space_center(&conn);
  krpc::services::Drawing drawing(&conn);
  auto vessel = space_center.active_vessel().value();

  // Draw the axes of the surface reference frame
  drawing.add_axes(vessel.surface_reference_frame());

  // Draw the direction of the surface velocity, as a labeled arrow
  auto ref_frame = vessel.surface_velocity_reference_frame();
  auto line = drawing.add_direction_from_com(std::make_tuple(0, 1, 0), ref_frame);
  line.set_end_arrow(true);
  line.set_label("Surface velocity");
  while (true) {
  }
}
