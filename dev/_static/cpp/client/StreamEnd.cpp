#include <iostream>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::KRPC krpc(&conn);
  krpc::services::SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel().value();
  auto temperature = vessel.parts().root().temperature_stream();

  try {
    temperature.acquire();
    while (true) {
      temperature.wait();
      std::cout << "Temperature " << temperature() << std::endl;
    }
  } catch (const krpc::services::KRPC::ObjectDestroyedException&) {
    std::cout << "The part was destroyed" << std::endl;
  }
  temperature.release();
}
