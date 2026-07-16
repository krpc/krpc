#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect("Navball directions");
  krpc::services::SpaceCenter space_center(&conn);
  auto vessel = space_center.active_vessel();
  auto ap = vessel.auto_pilot();
  ap.set_reference_frame(vessel.surface_reference_frame());
  ap.engage();

  // Point the vessel north on the navball, with a pitch of 0 degrees
  ap.set_target_direction(std::make_tuple(0, 1, 0));
  ap.wait();

  // Point the vessel vertically upwards on the navball
  ap.set_target_direction(std::make_tuple(1, 0, 0));
  ap.wait();

  // Point the vessel west (heading of 270 degrees), with a pitch of 0 degrees
  ap.set_target_direction(std::make_tuple(0, 0, -1));
  ap.wait();

  ap.disengage();
}
