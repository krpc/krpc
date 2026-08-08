import time
import krpc

conn = krpc.connect(name="User Interface Example")
canvas = conn.ui.stock_canvas

# Get the size of the game window in pixels
screen_size = canvas.rect_transform.size

# Add a panel to contain the UI elements. The user can drag it around the
# screen with the mouse.
panel = canvas.add_panel()
panel.draggable = True

# Position the panel on the left of the screen
rect = panel.rect_transform
rect.size = (200, 120)
rect.position = (110 - (screen_size[0] / 2), 0)

# Lay the contents of the panel out in a column
layout = panel.add_vertical_layout()
layout.padding = (10, 10, 10, 10)
layout.spacing = 6

# Add a button to set the throttle to maximum, with a tooltip shown while the
# mouse rests on it
button = panel.add_button("Full Throttle")
button.tooltip = "Set the throttle to maximum"
button.layout_element.preferred_size = (-1, 30)

# Add a slider to set the throttle by hand
slider = panel.add_slider()
slider.layout_element.preferred_size = (-1, 20)

# Add some text displaying the total engine thrust
text = panel.add_text("Thrust: 0 kN")
text.color = (1, 1, 1, 1)
text.size = 18

# Set up streams to monitor the button and the slider
button_clicked = conn.add_stream(getattr, button, "clicked")
slider_changed = conn.add_stream(getattr, slider, "changed")

vessel = conn.space_center.active_vessel
while True:
    # Handle the throttle button being clicked
    if button_clicked():
        vessel.control.throttle = 1
        slider.value = 1
        button.clicked = False

    # Handle the user moving the throttle slider
    if slider_changed():
        vessel.control.throttle = slider.value
        slider.changed = False

    # Update the thrust text
    text.content = "Thrust: %d kN" % (vessel.thrust / 1000)

    time.sleep(0.1)
