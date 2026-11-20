#include <iomanip>
#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <tuple>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  auto body = vessel.orbit().body();
  auto flight = vessel.flight(vessel.surface_reference_frame());
  // [docs]
  auto torque = flight.buoyant_torque_at(body.ocean_density(),
                                         body.surface_gravity());
  // [/docs]
  std::cout << std::fixed << std::setprecision(0) << "Torque = ("
            << std::get<0>(torque) << ", " << std::get<1>(torque) << ", "
            << std::get<2>(torque) << ") N m" << std::endl;
}
