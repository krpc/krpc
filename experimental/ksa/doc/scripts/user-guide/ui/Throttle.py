import time

import krpc

conn = krpc.connect(name="Throttle")
ksa = conn.ksa
vehicle = ksa.active_vehicle
# [docs]
window = ksa.UI.add_window("Throttle")
altitude = window.add_label("")
throttle = window.add_slider("Throttle", 0, 1)
row = window.add_row()
engine = row.add_toggle("Engine")
done = row.add_button("Done")

flight = vehicle.flight
while not done.clicked:
    altitude.content = f"Altitude: {flight.mean_altitude:.0f} m"
    if throttle.changed:
        throttle.changed = False
        vehicle.control.throttle = throttle.value
    if engine.changed:
        engine.changed = False
        vehicle.control.engine_on = engine.checked
    time.sleep(0.1)
window.remove()
# [/docs]
