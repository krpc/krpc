import time

import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight_info = vessel.flight()

# Each read gets the current altitude from the game
print(flight_info.mean_altitude)
time.sleep(1)
print(flight_info.mean_altitude)

# Two objects for the same vessel are equal
print(vessel == conn.space_center.active_vessel)
