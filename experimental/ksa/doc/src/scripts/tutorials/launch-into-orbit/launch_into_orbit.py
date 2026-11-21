"""Launch a rocket from Cape Canaveral into a circular orbit.

The script flies the ascent with a gravity turn, stages when the first
stage runs out of propellant, and circularizes with a burn that the flight
computer executes.
"""

import math
import time
from typing import Any

import krpc

# The turn spans most of Earth's atmosphere, so the rocket climbs out of the
# dense lower atmosphere before it pitches over far
TURN_START_ALTITUDE = 1_000
TURN_END_ALTITUDE = 150_000
TARGET_ALTITUDE = 500_000

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
available_thrust: Any = conn.add_stream(
    getattr, vehicle, "available_thrust"
)


def stage():
    """Separate the spent stage, and light the next one."""
    print("Staging")
    control.activate_next_sequence()
    control.activate_next_sequence()


def circularization_burn(ut):
    """Plan a burn at ut that makes the orbit circular, from the vis-viva
    equation."""
    mu = body.gravitational_parameter
    r = orbit.radius_at(ut)
    v1 = math.sqrt(mu * (2 / r - 1 / orbit.semi_major_axis))
    v2 = math.sqrt(mu / r)
    return vehicle.add_burn(ut, prograde=v2 - v1)


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
above_atmosphere = conn.add_event(
    lambda: flight.mean_altitude > atmosphere_altitude
)
with above_atmosphere.condition:
    above_atmosphere.wait()

print("Circularizing")
burn = circularization_burn(ksa.ut + orbit.time_to_apoapsis)
ksa.warp_to(burn.ignition_time - 30)
while True:
    auto_pilot.auto_burn = True
    finished = conn.add_event(lambda: not auto_pilot.auto_burn)
    with finished.condition:
        finished.wait()
    finished.remove()
    # Engines with propellant left mean the burn is complete
    if available_thrust() > 0:
        break
    # The game removes a burn that stopped part way, so we plan the rest
    # of it
    stage()
    if vehicle.available_thrust == 0:
        raise RuntimeError("Out of propellant")
    burn = circularization_burn(ksa.ut + 30)
burn.remove()

print(
    f"In orbit: apoapsis {orbit.apoapsis_altitude / 1000:.1f} km, "
    f"periapsis {orbit.periapsis_altitude / 1000:.1f} km"
)
