#include <iomanip>
#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  // [docs]
  auto body = vessel.orbit().body();
  auto flight = vessel.flight();
  auto displacement = flight.displacement();
  auto buoyancy =
      flight.buoyant_force_at(body.ocean_density(), body.surface_gravity());
  // [/docs]
  std::cout << std::fixed << std::setprecision(1)
            << "Displacement = " << displacement
            << " m^3, buoyancy = " << std::setprecision(0) << buoyancy
            << " N" << std::endl;
}
