import math
import time

import krpc

turn_start_altitude = 250
turn_end_altitude = 45000
target_altitude = 150000

conn = krpc.connect(name="Launch into orbit")
vessel = conn.space_center.active_vessel

# Set up streams for telemetry
ut = conn.add_stream(getattr, conn.space_center, "ut")
altitude = conn.add_stream(getattr, vessel.flight(), "mean_altitude")
apoapsis = conn.add_stream(getattr, vessel.orbit, "apoapsis_altitude")
stage_5_resources = vessel.decouple_stage_at(stage=5).resources(
    cumulative=False
)
stage_3_resources = vessel.decouple_stage_at(stage=3).resources(
    cumulative=False
)

# Pre-launch setup
vessel.control.sas = False
vessel.control.rcs = False
vessel.control.throttle = 1.0

# Activate the first stage
vessel.auto_pilot.engaged = True
vessel.auto_pilot.target_pitch_and_heading(90, 90)
vessel.control.activate_next_stage()


# Separate each stage when it runs out of fuel
def separate(event, name):
    event.remove()
    vessel.control.activate_next_stage()
    vessel.control.activate_next_stage()
    print(name, "stage separated")


first_stage_empty = conn.add_event(
    lambda: stage_5_resources.amount("SolidFuel") < 0.1
)
first_stage_empty.add_callback(lambda: separate(first_stage_empty, "First"))
second_stage_empty = conn.add_event(
    lambda: stage_3_resources.amount("LiquidFuel") < 0.1
)
second_stage_empty.add_callback(
    lambda: separate(second_stage_empty, "Second")
)

# Main ascent loop
while True:
    # Gravity turn
    if altitude() > turn_start_altitude and altitude() < turn_end_altitude:
        frac = (altitude() - turn_start_altitude) / (
            turn_end_altitude - turn_start_altitude
        )
        turn_angle = frac * 90
        vessel.auto_pilot.target_pitch_and_heading(90 - turn_angle, 90)

    # Stop the ascent near the target apoapsis
    if apoapsis() > target_altitude * 0.9:
        print("Approaching target apoapsis")
        break

    time.sleep(0.05)

# Fine tune the apoapsis at 25% thrust
vessel.control.throttle = 0.25
event = conn.add_event(
    lambda: vessel.orbit.apoapsis_altitude >= target_altitude
)
with event.condition:
    event.wait()
print("Target apoapsis reached")
vessel.control.throttle = 0.0

# Wait until out of atmosphere
print("Coasting out of atmosphere")
event = conn.add_event(lambda: vessel.flight().mean_altitude >= 70500)
with event.condition:
    event.wait()

# Plan circularization burn (using vis-viva equation)
print("Planning circularization burn")
mu = vessel.orbit.body.gravitational_parameter
r = vessel.orbit.apoapsis
a1 = vessel.orbit.semi_major_axis
a2 = r
v1 = math.sqrt(mu * ((2.0 / r) - (1.0 / a1)))
v2 = math.sqrt(mu * ((2.0 / r) - (1.0 / a2)))
delta_v = v2 - v1
node = vessel.control.add_node(
    ut() + vessel.orbit.time_to_apoapsis, prograde=delta_v
)

# Calculate burn time (using rocket equation)
F = vessel.available_thrust
Isp = vessel.specific_impulse * 9.82
m0 = vessel.mass
m1 = m0 / math.exp(delta_v / Isp)
flow_rate = F / Isp
burn_time = (m0 - m1) / flow_rate

# Orientate ship along the burn
print("Orientating ship for circularization burn")
frame = vessel.orbit.body.non_rotating_reference_frame
vessel.auto_pilot.reference_frame = frame
vessel.auto_pilot.target_direction = node.remaining_burn_vector(frame)
vessel.auto_pilot.wait()

# Wait until burn
print("Waiting until circularization burn")
burn_ut = ut() + vessel.orbit.time_to_apoapsis - (burn_time / 2.0)
lead_time = 5
conn.space_center.warp_to(burn_ut - lead_time)

# Execute burn
print("Ready to execute burn")
event = conn.add_event(
    lambda: vessel.orbit.time_to_apoapsis <= burn_time / 2.0
)
with event.condition:
    event.wait()
print("Executing burn")
remaining_burn = conn.add_stream(node.remaining_burn_vector, frame)
remaining_delta_v = conn.add_stream(getattr, node, "remaining_delta_v")
lowest_delta_v = remaining_delta_v()
while remaining_delta_v() > 0.1:
    # Steer along the remaining burn, which turns as the vessel moves
    # around its orbit
    vessel.auto_pilot.target_direction = remaining_burn()
    # Full throttle, then throttle down for the last few m/s
    vessel.control.throttle = 1.0 if remaining_delta_v() > 5 else 0.05
    # Stop if the burn overshoots, as the remaining delta-v then grows
    if remaining_delta_v() > lowest_delta_v + 0.1:
        break
    lowest_delta_v = min(lowest_delta_v, remaining_delta_v())
    time.sleep(0.05)
vessel.control.throttle = 0.0
node.remove()

print("Launch complete")
