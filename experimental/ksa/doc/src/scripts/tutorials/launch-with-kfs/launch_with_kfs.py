"""Launch a rocket with a kFS program flying the ascent.

The script loads ascent.c onto the rocket's flight computer and starts
it, then tells the program to lift off. The program flies the gravity
turn and reports its telemetry. The script reads the telemetry, and
asks the program to stage when the first stage runs dry.
"""

import time
from pathlib import Path
from typing import Any

import krpc

PROGRAM = Path(__file__).with_name("ascent.c")

# The phases of the program, as ascent.c numbers them
LAUNCH, ASCENT, COAST = 0, 1, 2

conn = krpc.connect(name="Launch with kFS")
ksa = conn.ksa
kfs = conn.kfs
kfs.available = True
vehicle = ksa.launch_vehicle("Rocket", "CCSFS LC-39A")

# The flight computer obeys a program only on a vehicle under control,
# so a kitten takes the Rocket's seat
kitten = next(
    k
    for k in ksa.kittens.values()
    if k.vehicle is None and not k.on_eva and not k.killed_in_action
)
vehicle.seats[0].kitten = kitten

# The game updates the rocket's control state at its next frame
deadline = time.monotonic() + 5
while vehicle.control_state != ksa.ControlState.controllable:
    if time.monotonic() > deadline:
        raise RuntimeError(
            f"Cannot control the rocket: {vehicle.control_state}"
        )
    time.sleep(0.1)

print("Loading the program")
computer = kfs.computer(vehicle)
computer.load({"ascent.c": PROGRAM.read_text()})
computer.start()


def telemetry(name: str) -> Any:
    """A stream of a field of the program's telemetry."""
    return conn.add_stream(computer.read_global, "telemetry." + name)


phase = telemetry("phase")
altitude = telemetry("altitude")
apoapsis = telemetry("apoapsis")
pitch = telemetry("pitch")
thrust = telemetry("thrust")
stages = telemetry("stages")

for count in range(3, 0, -1):
    print(f"{count}...")
    time.sleep(1)
print("Lift off")
computer.write_globals({"request.launch": 1})

state = conn.add_stream(getattr, computer, "state")
requested = 0
last_report = 0.0
while phase() != COAST:
    if state() == kfs.ComputerState.halted:
        raise RuntimeError(f"The program halted: {computer.trap}")
    # Stage once the program reports no thrust, and has done the
    # stagings asked for so far
    if phase() == ASCENT and thrust() == 0 and stages() == requested:
        requested += 1
        print(f"Staging at apoapsis {apoapsis() / 1000:.0f} km")
        computer.write_globals({"request.stages": requested})

    if time.monotonic() > last_report + 5:
        last_report = time.monotonic()
        print(
            f"Altitude {altitude() / 1000:.1f} km, "
            f"apoapsis {apoapsis() / 1000:.1f} km, "
            f"pitch {pitch():.0f}°"
        )
    time.sleep(0.1)

print("Target apoapsis reached")
print("Coasting out of the atmosphere")
body = vehicle.orbit.body
atmosphere_altitude = body.atmosphere_radius - body.radius
flight = vehicle.flight
above_atmosphere = conn.add_event(
    lambda: flight.mean_altitude > atmosphere_altitude
)
with above_atmosphere.condition:
    above_atmosphere.wait()

# Hand the rocket back to the pilot
computer.stop()
print("The program's log:")
for line in computer.log:
    print("  " + line)

orbit = vehicle.orbit
print(
    f"Apoapsis {orbit.apoapsis_altitude / 1000:.1f} km, "
    f"periapsis {orbit.periapsis_altitude / 1000:.1f} km"
)
