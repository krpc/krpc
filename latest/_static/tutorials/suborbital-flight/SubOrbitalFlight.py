import time

import krpc

conn = krpc.connect(name="Sub-orbital flight")

vessel = conn.space_center.active_vessel

vessel.auto_pilot.target_pitch_and_heading(90, 90)
vessel.auto_pilot.engaged = True
vessel.control.throttle = 0.5
time.sleep(1)

print("Launch!")
vessel.control.activate_next_stage()

flight = vessel.flight()
event = conn.add_event(lambda: flight.mean_altitude > 500)
with event.condition:
    event.wait()

print("Pitching over")
vessel.auto_pilot.target_smoothing_time = 10
vessel.auto_pilot.target_pitch_and_heading(70, 90)

event = conn.add_event(lambda: vessel.resources.amount("LiquidFuel") < 0.1)
with event.condition:
    event.wait()

print("Flame out")
vessel.control.throttle = 0
vessel.auto_pilot.reference_frame = vessel.surface_velocity_reference_frame
vessel.auto_pilot.target_direction = (0, 1, 0)

body_flight = vessel.flight(vessel.orbit.body.reference_frame)
event = conn.add_event(lambda: body_flight.vertical_speed < 0)
with event.condition:
    event.wait()

print("Apoapsis reached, separating")
vessel.control.activate_next_stage()
vessel.auto_pilot.target_direction = (0, -1, 0)
vessel.auto_pilot.wait()
vessel.auto_pilot.engaged = False

event = conn.add_event(lambda: flight.surface_altitude < 2000)
with event.condition:
    event.wait()

print("Deploying the parachute")
vessel.parts.parachutes[0].deploy_altitude = 550
vessel.control.activate_next_stage()

situation = conn.space_center.VesselSituation
while vessel.situation not in (situation.landed, situation.splashed):
    print("Altitude = %.1f meters" % flight.surface_altitude)
    time.sleep(1)
print("Landed!")
