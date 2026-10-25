"""Circularize the active vehicle's orbit at its apoapsis.

The script plans the burn, and the vehicle's flight computer executes it.
"""

import math

import krpc

conn = krpc.connect(name="Circularize")
ksa = conn.ksa
vehicle = ksa.active_vehicle
orbit = vehicle.orbit
auto_pilot = vehicle.auto_pilot

# The change in speed at apoapsis that makes the orbit circular
mu = orbit.body.gravitational_parameter
r = orbit.apoapsis
v1 = math.sqrt(mu * (2 / r - 1 / orbit.semi_major_axis))
v2 = math.sqrt(mu / r)
burn = vehicle.add_burn(ksa.ut + orbit.time_to_apoapsis, prograde=v2 - v1)
print(f"Circularization burn: {burn.delta_v:.1f} m/s, {burn.burn_time:.1f} s")

print("Coasting to the burn")
ksa.warp_to(burn.ignition_time - 30)

print("Executing the burn")
auto_pilot.auto_burn = True
auto_burn = conn.add_stream(getattr, auto_pilot, "auto_burn")
with auto_burn.condition:
    while auto_burn():
        auto_burn.wait()
burn.remove()

print(
    f"Apoapsis {orbit.apoapsis_altitude / 1000:.1f} km, "
    f"periapsis {orbit.periapsis_altitude / 1000:.1f} km"
)
