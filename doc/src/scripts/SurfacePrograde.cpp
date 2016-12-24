#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect("Surface prograde");
  krpc::services::SpaceCenter spaceCenter(&conn);
  auto vessel = spaceCenter.active_vessel();
  auto ap = vessel.auto_pilot();

  ap.set_reference_frame(vessel.surface_velocity_reference_frame());
  ap.set_target_direction(std::make_tuple(0.0, 1.0, 0.0));
  ap.engage();
  ap.wait();
  ap.disengage();
}
