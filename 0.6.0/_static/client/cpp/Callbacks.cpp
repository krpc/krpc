#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

void check_abort1(bool x) {
  std::cout << "Abort 1 called with a value of " << x << std::endl;
}

void check_abort2(bool x) {
  std::cout << "Abort 2 called with a value of " << x << std::endl;
}

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto control = sc.active_vessel().control();
  auto abort = control.abort_stream();

  abort.add_callback(check_abort1);
  abort.add_callback(check_abort2);
  abort.start();

  // Keep the program running...
  while (true) {
  }
}
