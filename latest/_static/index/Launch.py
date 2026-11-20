import krpc

conn = krpc.connect(name="Launch")
vessel = conn.space_center.active_vessel
flight = vessel.flight()

vessel.auto_pilot.target_pitch_and_heading(90, 90)
vessel.auto_pilot.engaged = True
vessel.control.throttle = 1
vessel.control.activate_next_stage()
print(f"{vessel.name} has launched")

event = conn.add_event(lambda: flight.mean_altitude > 10000)
with event.condition:
    event.wait()

vessel.control.throttle = 0
vessel.auto_pilot.engaged = False
print(f"{vessel.name} reached 10 km")
