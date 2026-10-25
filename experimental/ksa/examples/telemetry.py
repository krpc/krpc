"""Print telemetry for the active vehicle in KSA, once a second.

The KSA service has no pre-generated stubs, so the client builds it from the
definitions the server sends when it connects.
"""

import time

import krpc

conn = krpc.connect(name="KSA telemetry")
ksa = conn.ksa  # pyright: ignore[reportAttributeAccessIssue]
print(f"Bodies: {', '.join(sorted(ksa.bodies))}")

vehicle = ksa.active_vehicle
print(f"Active vehicle: {vehicle.name} ({vehicle.situation})")

flight = vehicle.flight
orbit = vehicle.orbit
altitude = conn.add_stream(getattr, flight, "mean_altitude")
apoapsis = conn.add_stream(getattr, orbit, "apoapsis_altitude")

while True:
    print(
        f"UT {ksa.ut:.0f} s  altitude {altitude():.0f} m  "
        f"apoapsis {apoapsis():.0f} m  speed {flight.speed:.1f} m/s  "
        f"pitch {flight.pitch:.1f} heading {flight.heading:.1f} roll {flight.roll:.1f}  "
        f"delta-v {vehicle.delta_v:.0f} m/s"
    )
    time.sleep(1)
