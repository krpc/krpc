#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter space_center(conn);
  auto vessel = space_center.active_vessel().value();
  // [docs]
  for (auto part : vessel.parts().all())
    if (part.static_pressure() + part.dynamic_pressure() >
        part.max_pressure())
      std::cout << part.title() << " is over its pressure limit"
                << std::endl;
  // [/docs]
}
