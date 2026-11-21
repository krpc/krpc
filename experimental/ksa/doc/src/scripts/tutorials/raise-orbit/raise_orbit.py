"""Raise the active vehicle's circular orbit to a higher circular orbit.

The first burn raises the apoapsis to the target altitude. The second burn, at the
apoapsis, raises the periapsis to match.
"""

import math

import krpc

TARGET_ALTITUDE = 400_000

conn = krpc.connect(name="Raise orbit")
ksa = conn.ksa
vehicle = ksa.active_vehicle
orbit = vehicle.orbit
control = vehicle.control
auto_pilot = vehicle.auto_pilot

print("Pointing prograde")
auto_pilot.mode = ksa.AutoPilotMode.prograde
auto_pilot.engage()
auto_pilot.wait()

print("Raising apoapsis")
control.throttle = 1
control.engine_on = True
apoapsis_reached = conn.add_event(lambda: orbit.apoapsis_altitude > TARGET_ALTITUDE)
with apoapsis_reached.condition:
    apoapsis_reached.wait()
control.engine_on = False

# The circularization burn, from the vis-viva equation
mu = orbit.body.gravitational_parameter
r = orbit.apoapsis
v1 = math.sqrt(mu * (2 / r - 1 / orbit.semi_major_axis))
v2 = math.sqrt(mu / r)
delta_v = v2 - v1

# The burn time, from the rocket equation
exhaust_velocity = vehicle.specific_impulse * 9.80665
initial_mass = vehicle.mass
final_mass = initial_mass / math.exp(delta_v / exhaust_velocity)
mass_flow_rate = vehicle.available_thrust / exhaust_velocity
burn_time = (initial_mass - final_mass) / mass_flow_rate
print(f"Circularization burn: {delta_v:.1f} m/s, {burn_time:.1f} s")

print("Coasting to apoapsis")
burn_ut = ksa.ut + orbit.time_to_apoapsis - burn_time / 2
ksa.warp_to(burn_ut - 10)
auto_pilot.wait()
burn_start = conn.add_event(lambda: ksa.ut >= burn_ut)
with burn_start.condition:
    burn_start.wait()

print("Raising periapsis")
control.engine_on = True
burn_end = burn_ut + burn_time
burn_done = conn.add_event(
    lambda: ksa.ut >= burn_end or orbit.periapsis_altitude >= TARGET_ALTITUDE
)
with burn_done.condition:
    burn_done.wait()
control.engine_on = False
auto_pilot.disengage()

print(
    f"Apoapsis {orbit.apoapsis_altitude / 1000:.1f} km, "
    f"periapsis {orbit.periapsis_altitude / 1000:.1f} km"
)
