#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto control = sc.active_vessel().control();
  auto abort = control.abort_stream();
  abort.acquire();
  while (!abort())
    abort.wait();
  abort.release();
}
