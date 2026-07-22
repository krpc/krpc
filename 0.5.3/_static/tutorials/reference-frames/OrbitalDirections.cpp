#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect("Orbital directions");
  krpc::services::SpaceCenter space_center(&conn);
  auto vessel = space_center.active_vessel();
  auto ap = vessel.auto_pilot();
  ap.set_reference_frame(vessel.orbital_reference_frame());
  ap.engage();

  // Point the vessel in the prograde direction
  ap.set_target_direction(std::make_tuple(0, 1, 0));
  ap.wait();

  // Point the vessel in the orbit normal direction
  ap.set_target_direction(std::make_tuple(0, 0, 1));
  ap.wait();

  // Point the vessel in the orbit radial direction
  ap.set_target_direction(std::make_tuple(-1, 0, 0));
  ap.wait();

  ap.disengage();
}
