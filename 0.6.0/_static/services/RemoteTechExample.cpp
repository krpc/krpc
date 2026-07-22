#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/remote_tech.hpp>

int main() {
  krpc::Client conn = krpc::connect("RemoteTech Example");
  krpc::services::SpaceCenter space_center(&conn);
  krpc::services::RemoteTech remote_tech(&conn);
  auto vessel = space_center.active_vessel();

  // Set a dish target
  auto part = vessel.parts().with_title("Reflectron KR-7").front();
  auto antenna = remote_tech.antenna(part);
  antenna.set_target_body(space_center.bodies()["Jool"]);

  // Get info about the vessels communications
  auto comms = remote_tech.comms(vessel);
  std::cout << "Signal delay = " << comms.signal_delay() << std::endl;
}
