#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  auto flight_info = sc.active_vessel().value().flight();
  auto altitude = flight_info.mean_altitude_stream();
  // Update the stream at most 10 times a second
  altitude.set_rate(10);
}
