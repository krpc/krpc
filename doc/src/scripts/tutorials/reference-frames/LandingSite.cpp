#include <iostream>
#include <iomanip>
#include <cmath>
#include <chrono>
#include <thread>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/ui.hpp>
#include <krpc/services/drawing.hpp>

static const double pi = 3.1415926535897;

int main() {
  krpc::Client conn = krpc::connect("Landing Site");
  krpc::services::SpaceCenter space_center(&conn);
  krpc::services::Drawing drawing(&conn);
  auto vessel = space_center.active_vessel();
  auto body = vessel.orbit().body();

  // Define the landing site as the top of the VAB
  double landing_latitude = -(0.0+(5.0/60.0)+(48.38/60.0/60.0));
  double landing_longitude = -(74.0+(37.0/60.0)+(12.2/60.0/60.0));
  double landing_altitude = 111;

  // Determine landing site reference frame
  // (orientation: x=zenith, y=north, z=east)
  auto landing_position = body.surface_position(
    landing_latitude, landing_longitude, body.reference_frame());
  auto q_long = std::make_tuple(
    0.0,
    sin(-landing_longitude * 0.5 * pi / 180.0),
    0.0,
    cos(-landing_longitude * 0.5 * pi / 180.0)
    );
  auto q_lat = std::make_tuple(
    0.0,
    0.0,
    sin(landing_latitude * 0.5 * pi / 180.0),
    cos(landing_latitude * 0.5 * pi / 180.0)
  );
  auto landing_reference_frame =
    krpc::services::SpaceCenter::ReferenceFrame::create_relative(
      conn,
      krpc::services::SpaceCenter::ReferenceFrame::create_relative(
        conn,
        krpc::services::SpaceCenter::ReferenceFrame::create_relative(
          conn,
          body.reference_frame(),
          landing_position,
          q_long),
        std::make_tuple(0, 0, 0),
        q_lat),
      std::make_tuple(landing_altitude, 0, 0));

  // Draw axes
  drawing.add_line(
    std::make_tuple(0, 0, 0), std::make_tuple(1, 0, 0), landing_reference_frame);
  drawing.add_line(
    std::make_tuple(0, 0, 0), std::make_tuple(0, 1, 0), landing_reference_frame);
  drawing.add_line(
    std::make_tuple(0, 0, 0), std::make_tuple(0, 0, 1), landing_reference_frame);

  while (true)
    std::this_thread::sleep_for(std::chrono::seconds(1));
}
