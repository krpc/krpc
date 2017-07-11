#include <iostream>
#include <iomanip>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client connection = krpc::connect("Vessel velocity");
  krpc::services::SpaceCenter spaceCenter(&connection);
  auto vessel = spaceCenter.active_vessel();
  auto ref_frame = krpc::services::SpaceCenter::ReferenceFrame::create_hybrid(
    connection,
    vessel.orbit().body().reference_frame(),
    vessel.surface_reference_frame()
  );

  while (true) {
    auto velocity = vessel.flight(ref_frame).velocity();
    std::cout
      << std::fixed << std::setprecision(1)
      << "Surface velocity = ("
      << std::get<0>(velocity) << ","
      << std::get<1>(velocity) << ","
      << std::get<2>(velocity)
      << ")" << std::endl;
    std::this_thread::sleep_for(std::chrono::seconds(1));
  }
}
