#include <chrono>
#include <string>
#include <thread>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/ui.hpp>
#include <krpc/services/drawing.hpp>

using SpaceCenter = krpc::services::SpaceCenter;
using Drawing = krpc::services::Drawing;

int main() {
  krpc::Client conn = krpc::connect("Drawing Example");
  SpaceCenter space_center(&conn);
  Drawing drawing(&conn);
  auto vessel = space_center.active_vessel().value();
  auto kerbin = space_center.bodies()["Kerbin"];

  // Draw a circular orbit 250 km above Kerbin, as a dashed green path with
  // a label
  auto orbit = SpaceCenter::Orbit::create_from_orbital_elements(
    conn, kerbin, kerbin.equatorial_radius() + 250000, 0, 0, 0, 0, 0,
    space_center.ut());
  auto path = drawing.add_orbit(orbit);
  path.set_color(std::make_tuple(0, 1, 0));
  path.set_dashed(true);
  path.set_label("Target orbit");

  // Mark the north pole in the map view, with the game's flag icon
  auto site = kerbin.surface_position(90, 0, kerbin.reference_frame());
  drawing.add_map_marker(
    site, kerbin.reference_frame(), Drawing::MapMarkerIcon::flag, "North pole");

  // Draw a yellow arrow from the vessel, pointing prograde
  auto arrow = drawing.add_direction_from_com(
    std::make_tuple(0, 1, 0), vessel.orbital_reference_frame());
  arrow.set_color(std::make_tuple(1, 1, 0));
  arrow.set_end_arrow(true);

  // Label the arrow with the orbital speed of the vessel
  while (true) {
    arrow.set_label(std::to_string((int)vessel.orbit().speed()) + " m/s");
    std::this_thread::sleep_for(std::chrono::seconds(1));
  }
}
