import math

import krpc

conn = krpc.connect(name="Direction")
ksa = conn.ksa
vehicle = ksa.active_vehicle
# [docs]
east, north, up = vehicle.direction(vehicle.surface_reference_frame)
pitch = math.degrees(math.asin(up))
heading = math.degrees(math.atan2(east, north)) % 360
print(f"Pitch {pitch:.1f}, heading {heading:.1f}")
flight = vehicle.flight
print(f"Flight reports {flight.pitch:.1f}, {flight.heading:.1f}")
# [/docs]
