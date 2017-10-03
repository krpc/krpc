#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect();
  krpc::services::SpaceCenter space_center(&conn);
  auto vessel = space_center.active_vessel();
  auto part = vessel.parts().with_title("Clamp-O-Tron Docking Port").front();
  vessel.parts().set_controlling(part);
}
