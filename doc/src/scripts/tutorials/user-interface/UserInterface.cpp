#include <chrono>
#include <thread>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <krpc/services/ui.hpp>

int main() {
  krpc::Client conn = krpc::connect("User Interface Example");
  krpc::services::SpaceCenter space_center(&conn);
  krpc::services::UI ui(&conn);
  auto canvas = ui.stock_canvas();

  // Get the size of the game window in pixels
  auto screen_size = canvas.rect_transform().size();

  // Add a panel to contain the UI elements. The user can drag it around the
  // screen with the mouse.
  auto panel = canvas.add_panel();
  panel.set_draggable(true);

  // Position the panel on the left of the screen
  auto rect = panel.rect_transform();
  rect.set_size(std::make_tuple(200, 120));
  rect.set_position(std::make_tuple(110-(std::get<0>(screen_size)/2), 0));

  // Lay the contents of the panel out in a column
  auto layout = panel.add_vertical_layout();
  layout.set_padding(std::make_tuple(10, 10, 10, 10));
  layout.set_spacing(6);

  // Add a button to set the throttle to maximum, with a tooltip shown while
  // the mouse rests on it
  auto button = panel.add_button("Full Throttle");
  button.set_tooltip("Set the throttle to maximum");
  button.layout_element().set_preferred_size(std::make_tuple(-1, 30));

  // Add a slider to set the throttle by hand
  auto slider = panel.add_slider();
  slider.layout_element().set_preferred_size(std::make_tuple(-1, 20));

  // Add some text displaying the total engine thrust
  auto text = panel.add_text("Thrust: 0 kN");
  text.set_color(std::make_tuple(1, 1, 1, 1));
  text.set_size(18);

  // Set up streams to monitor the button and the slider
  auto button_clicked = button.clicked_stream();
  auto slider_changed = slider.changed_stream();

  auto vessel = space_center.active_vessel().value();
  while (true) {
    // Handle the throttle button being clicked
    if (button_clicked()) {
      vessel.control().set_throttle(1);
      slider.set_value(1);
      button.set_clicked(false);
    }

    // Handle the user moving the throttle slider
    if (slider_changed()) {
      vessel.control().set_throttle(slider.value());
      slider.set_changed(false);
    }

    // Update the thrust text
    text.set_content("Thrust: " + std::to_string((int)(vessel.thrust()/1000)) + " kN");

    std::this_thread::sleep_for(std::chrono::seconds(1));
  }
}
