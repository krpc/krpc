#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect();
  krpc::services::SpaceCenter space_center(&conn);
  auto vessel = space_center.active_vessel();
  for (auto parachute : vessel.parts().parachutes())
    parachute.deploy();
}
