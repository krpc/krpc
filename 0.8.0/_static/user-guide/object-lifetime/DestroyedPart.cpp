#include <iostream>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  auto part = vessel.parts().all()[0];
  // [docs]
  try {
    std::cout << part.temperature() << std::endl;
  } catch (const krpc::services::KRPC::ObjectDestroyedException&) {
    std::cout << "that part is gone" << std::endl;
  }
  // [/docs]
}
