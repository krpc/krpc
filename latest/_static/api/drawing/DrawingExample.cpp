#include <chrono>
#include <krpc.hpp>
#include <krpc/services/drawing.hpp>
#include <krpc/services/space_center.hpp>
#include <thread>
#include <tuple>

int main() {
  auto conn = krpc::connect("Drawing Example");
  krpc::services::SpaceCenter space_center(conn);
  krpc::services::Drawing drawing(conn);
  auto vessel = space_center.active_vessel().value();
  drawing.add_direction_from_com(std::make_tuple(0, 1, 0),
                                 vessel.reference_frame());
  std::this_thread::sleep_for(std::chrono::seconds(10));
}
