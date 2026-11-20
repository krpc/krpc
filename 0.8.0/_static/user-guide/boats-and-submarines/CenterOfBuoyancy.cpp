#include <iomanip>
#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <tuple>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  // [docs]
  auto flight = vessel.flight(vessel.surface_reference_frame());
  auto center_of_buoyancy = flight.center_of_buoyancy();
  auto center_of_mass = flight.center_of_mass();
  // [/docs]
  if (center_of_buoyancy) {
    double north =
        std::get<1>(*center_of_buoyancy) - std::get<1>(center_of_mass);
    double east =
        std::get<2>(*center_of_buoyancy) - std::get<2>(center_of_mass);
    std::cout << std::fixed << std::setprecision(2)
              << "Trim north = " << north << " m, east = " << east << " m"
              << std::endl;
  }
}
