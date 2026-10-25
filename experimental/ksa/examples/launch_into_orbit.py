"""Launch a rocket from Cape Canaveral into a circular orbit.

The script flies the ascent with a gravity turn, stages when the first stage runs out of
propellant, and circularizes with a burn that the flight computer executes.
"""

import math
import time
from typing import Any

import krpc

# The turn spans most of Earth's atmosphere, so the rocket climbs out of the dense lower
# atmosphere before it pitches over far
TURN_START_ALTITUDE = 1_000
TURN_END_ALTITUDE = 150_000
TARGET_ALTITUDE = 200_000

conn = krpc.connect(name="Launch into orbit")
ksa = conn.ksa
vehicle = ksa.launch_vehicle("Rocket", "CCSFS LC-39A")
control = vehicle.control
auto_pilot = vehicle.auto_pilot
flight = vehicle.flight
orbit = vehicle.orbit
body = orbit.body

# Streams for the values the ascent loop reads
altitude: Any = conn.add_stream(getattr, flight, "mean_altitude")
apoapsis: Any = conn.add_stream(getattr, orbit, "apoapsis_altitude")
available_thrust: Any = conn.add_stream(getattr, vehicle, "available_thrust")


def stage():
    """Separate the spent stage, and light the next one."""
    print("Staging")
    control.activate_next_sequence()
    control.activate_next_sequence()


def execute_burn(burn):
    """Execute a burn with the flight computer, staging when a stage runs dry."""
    while True:
        auto_pilot.auto_burn = True
        # A stream added after the change starts from the new value
        auto_burn = conn.add_stream(getattr, auto_pilot, "auto_burn")
        with auto_burn.condition:
            while auto_burn():
                auto_burn.wait()
        auto_burn.remove()
        # The flight computer stops the burn when the engines run out of propellant
        if burn.remaining_delta_v < 1:
            break
        stage()


print("Launching")
auto_pilot.target_pitch_and_heading(90, 90)
auto_pilot.engage()
control.throttle = 1
control.activate_next_sequence()
control.engine_on = True

while apoapsis() < TARGET_ALTITUDE:
    # Pitch over gradually from vertical to horizontal, heading east
    if altitude() > TURN_START_ALTITUDE:
        fraction = (altitude() - TURN_START_ALTITUDE) / (
            TURN_END_ALTITUDE - TURN_START_ALTITUDE
        )
        auto_pilot.target_pitch = 90 * (1 - math.sqrt(min(fraction, 1)))

    # Separate the first stage and light the second once the first runs dry
    if available_thrust() == 0:
        stage()

    time.sleep(0.1)

print("Target apoapsis reached")
control.engine_on = False

print("Coasting out of the atmosphere")
atmosphere_altitude = body.atmosphere_radius - body.radius
while altitude() < atmosphere_altitude:
    time.sleep(1)

print("Circularizing")
mu = body.gravitational_parameter
r = orbit.apoapsis
v1 = math.sqrt(mu * (2 / r - 1 / orbit.semi_major_axis))
v2 = math.sqrt(mu / r)
burn = vehicle.add_burn(ksa.ut + orbit.time_to_apoapsis, prograde=v2 - v1)
ksa.warp_to(burn.ignition_time - 30)
execute_burn(burn)
burn.remove()

print(
    f"In orbit: apoapsis {orbit.apoapsis_altitude / 1000:.1f} km, "
    f"periapsis {orbit.periapsis_altitude / 1000:.1f} km"
)
