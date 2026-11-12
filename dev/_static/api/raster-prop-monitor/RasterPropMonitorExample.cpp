#include <algorithm>
#include <krpc.hpp>
#include <krpc/services/raster_prop_monitor.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Connection conn = krpc::connect("Raster Prop Monitor Example");
  krpc::services::SpaceCenter sc(conn);
  krpc::services::RasterPropMonitor rpm(conn);
  auto vessel = sc.active_vessel().value();
  auto control = vessel.control();

  // The first screen in the IVA, and the content of its krpc page
  auto monitor = rpm.monitors(vessel)[0];
  auto display = monitor.display();

  // Raster Prop Monitor formats the SAS variable, and keeps it up to date
  display.set_text("SAS: {0:ON;OFF;OFF} $&$ SAS");

  // A press of the NAV button shows the page
  monitor.capture_button("buttonR1", "krpc");

  // Each press of the ENTER button toggles SAS
  auto buttons = monitor.buttons();
  auto enter = std::find(buttons.begin(), buttons.end(), "button_ENTER");
  auto pressed = display.button_pressed(enter - buttons.begin());
  pressed.acquire();
  while (true) {
    pressed.wait();
    control.set_sas(!control.sas());
  }
}
