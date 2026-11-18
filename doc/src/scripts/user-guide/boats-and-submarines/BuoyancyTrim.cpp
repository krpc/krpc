#include <cmath>
#include <cstdio>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <stdexcept>
#include <tuple>

int main() {
  auto conn = krpc::connect("Buoyancy Trim");
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  auto body = vessel.orbit().body();

  if (!body.has_ocean())
    throw std::runtime_error(body.name() + " has no ocean");

  auto flight = vessel.flight(vessel.surface_reference_frame());

  // The fully submerged figures, which do not depend on where the vessel
  // is
  auto buoyancy =
      flight.buoyant_force_at(body.ocean_density(), body.surface_gravity());
  auto torque = flight.buoyant_torque_at(body.ocean_density(),
                                         body.surface_gravity());
  auto weight = vessel.mass() * body.surface_gravity();

  std::printf("Displacement:  %.1f m^3\n", flight.displacement());
  std::printf("Buoyancy:      %.0f N fully submerged\n", buoyancy);
  std::printf("Weight:        %.0f N\n", weight);
  std::printf("Floats:        %s\n", buoyancy > weight ? "true" : "false");
  std::printf("Torque:        %.0f N m fully submerged\n",
              std::hypot(std::get<0>(torque), std::get<1>(torque),
                         std::get<2>(torque)));

  // The trim, read in the surface reference frame. Its x axis points up,
  // so the y and z axes span the horizontal. A horizontal offset between
  // the two centers tips the hull until they line up again.
  auto center_of_buoyancy = flight.center_of_buoyancy();

  if (!center_of_buoyancy) {
    std::printf("Trim:          vessel is out of the water\n");
  } else {
    auto center_of_mass = flight.center_of_mass();
    auto north =
        std::get<1>(*center_of_buoyancy) - std::get<1>(center_of_mass);
    auto east =
        std::get<2>(*center_of_buoyancy) - std::get<2>(center_of_mass);
    std::printf("Submerged:     %.0f%%\n",
                100 * flight.submerged_portion());
    std::printf("Trim:          %.2f m horizontally\n",
                std::hypot(north, east));
    std::printf("Trim north:    %+.2f m\n", north);
    std::printf("Trim east:     %+.2f m\n", east);
  }
}
