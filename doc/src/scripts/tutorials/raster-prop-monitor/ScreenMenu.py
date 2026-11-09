import queue

import krpc

conn = krpc.connect(name="Screen Menu")
vessel = conn.space_center.active_vessel
control = vessel.control
rpm = conn.raster_prop_monitor

# The first screen in the vessel's IVA, and the page the script draws on it
monitor = rpm.monitors(vessel)[0]
display = monitor.display

# The controls the menu toggles, and the item the cursor is on
items = ["lights", "gear", "brakes", "sas"]
cursor = 0


def draw():
    lines = ["[#00ff00]kRPC MENU", ""]
    for i, item in enumerate(items):
        mark = "[#ffff00]>" if i == cursor else " "
        state = "[#00ff00]ON" if getattr(control, item) else "[#ff4040]OFF"
        lines.append("%s %-8s %s[#ffffff]" % (mark, item.upper(), state))
    # Raster Prop Monitor formats this line, and keeps it up to date
    lines += ["", "ALT {0:SIP6.1}m $&$ ALTITUDE"]
    display.text = "\n".join(lines)


# Button presses arrive on the client's stream thread, so the callbacks
# queue them for the loop below
presses = queue.Queue()
buttons = monitor.buttons
up, down, enter = (
    buttons.index(name) for name in ("button_UP", "button_DOWN", "button_ENTER")
)
for button in (up, down, enter):
    event = display.button_pressed(button)
    event.add_callback(lambda button=button: presses.put(button))

# Show the menu, and make the NAV button show it too, in place of
# Raster Prop Monitor's navigation page
draw()
monitor.active_page = "krpc"
monitor.capture_button("buttonR1", "krpc")

try:
    while True:
        button = presses.get()
        if button == up:
            cursor = (cursor - 1) % len(items)
        elif button == down:
            cursor = (cursor + 1) % len(items)
        else:
            item = items[cursor]
            setattr(control, item, not getattr(control, item))
        draw()
finally:
    monitor.release_capture("buttonR1")
    monitor.active_page = "home"
