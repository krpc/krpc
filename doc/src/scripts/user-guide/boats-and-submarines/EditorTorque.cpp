#include <cmath>
#include <iomanip>
#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <tuple>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto body = space_center.bodies()["Kerbin"];
  typedef krpc::services::SpaceCenter::ReferenceFrame ReferenceFrame;
  // [docs]
  auto vessel = space_center.editor().vessel();
  auto flight = vessel.flight(vessel.reference_frame());
  // The vessel frame's z axis points out of the bottom, so up is its -z
  // axis
  double side = std::sqrt(0.5);
  flight.set_surface_reference_frame(ReferenceFrame::create_relative(
      conn, vessel.reference_frame(), std::make_tuple(0.0, 0.0, 0.0),
      std::make_tuple(0.0, side, 0.0, side)));
  auto torque = flight.buoyant_torque_at(body.ocean_density(),
                                         body.surface_gravity());
  // [/docs]
  std::cout << std::fixed << std::setprecision(0) << "Torque = ("
            << std::get<0>(torque) << ", " << std::get<1>(torque) << ", "
            << std::get<2>(torque) << ") N m" << std::endl;
}
