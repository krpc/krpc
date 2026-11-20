#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Connection conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  for (auto parachute : vessel.parts().parachutes()) parachute.deploy();
}
