#include <chrono>
#include <thread>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/ui.hpp>
#include <krpc/services/drawing.hpp>

int main() {
  krpc::Client conn = krpc::connect("Landing Site");
  krpc::services::SpaceCenter space_center(&conn);
  krpc::services::Drawing drawing(&conn);
  auto vessel = space_center.active_vessel().value();
  auto body = vessel.orbit().body();

  // Define the landing site as the top of the VAB
  double landing_latitude = -(0.0+(5.0/60.0)+(48.38/60.0/60.0));
  double landing_longitude = -(74.0+(37.0/60.0)+(12.2/60.0/60.0));
  double landing_altitude = 111;

  // Determine landing site reference frame
  // (orientation: x=zenith, y=north, z=east)
  auto landing_reference_frame = body.surface_reference_frame(
    landing_latitude, landing_longitude, landing_altitude);

  // Draw axes
  drawing.add_axes(landing_reference_frame);

  while (true)
    std::this_thread::sleep_for(std::chrono::seconds(1));
}
