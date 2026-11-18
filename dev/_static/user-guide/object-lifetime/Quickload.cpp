#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  // [docs]
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  auto parachute =
      vessel.parts().with_name("parachuteSingle")[0].parachute().value();
  space_center.quickload();
  // reads the parachute of the loaded game
  std::cout << static_cast<int>(parachute.state()) << std::endl;
  // [/docs]
}
