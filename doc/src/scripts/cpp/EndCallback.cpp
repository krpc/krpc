#include <chrono>
#include <exception>
#include <iostream>
#include <thread>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

void print_temperature(double value) {
  std::cout << "Temperature " << value << std::endl;
}

void print_ending(std::exception_ptr error) {
  try {
    std::rethrow_exception(error);
  } catch (const std::exception& exn) {
    std::cout << "The stream ended: " << exn.what() << std::endl;
  }
}

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel().value();
  auto temperature = vessel.parts().root().temperature_stream();

  temperature.add_callback(print_temperature);
  temperature.add_end_callback(print_ending);

  // Keep the program running, while the callbacks are called
  while (true)
    std::this_thread::sleep_for(std::chrono::seconds(1));
}
