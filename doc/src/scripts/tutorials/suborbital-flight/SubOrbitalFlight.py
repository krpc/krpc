import time
import krpc

conn = krpc.connect(name="Sub-orbital flight")

vessel = conn.space_center.active_vessel

vessel.auto_pilot.target_pitch_and_heading(90, 90)
vessel.auto_pilot.engaged = True
vessel.control.throttle = 1
time.sleep(1)

print("Launch!")
vessel.control.activate_next_stage()

event = conn.add_event(lambda: vessel.resources.amount("SolidFuel") < 0.1)
with event.condition:
    event.wait()
print("Booster separation")
vessel.control.activate_next_stage()

flight = vessel.flight()
event = conn.add_event(lambda: flight.mean_altitude > 10000)
with event.condition:
    event.wait()

print("Gravity turn")
vessel.auto_pilot.target_pitch_and_heading(60, 90)

event = conn.add_event(lambda: vessel.orbit.apoapsis_altitude > 100000)
with event.condition:
    event.wait()

print("Launch stage separation")
vessel.control.throttle = 0
time.sleep(1)
vessel.control.activate_next_stage()
vessel.auto_pilot.engaged = False

event = conn.add_event(lambda: flight.surface_altitude < 1000)
with event.condition:
    event.wait()

vessel.control.activate_next_stage()

while vessel.flight(vessel.orbit.body.reference_frame).vertical_speed < -0.1:
    print("Altitude = %.1f meters" % flight.surface_altitude)
    time.sleep(1)
print("Landed!")
