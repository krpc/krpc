import krpc

conn = krpc.connect(name="Raster Prop Monitor Example")
vessel = conn.space_center.active_vessel
control = vessel.control
rpm = conn.raster_prop_monitor

# The first screen in the IVA, and the content of its krpc page
monitor = rpm.monitors(vessel)[0]
display = monitor.display

# Raster Prop Monitor formats the SAS variable, and keeps it up to date
display.text = "SAS: {0:ON;OFF;OFF} $&$ SAS"

# A press of the NAV button shows the page
monitor.capture_button("buttonR1", "krpc")

# Each press of the ENTER button toggles SAS
enter = monitor.buttons.index("button_ENTER")
pressed = display.button_pressed(enter)
with pressed.condition:
    while True:
        pressed.wait()
        control.sas = not control.sas
