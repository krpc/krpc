import time

import krpc

conn = krpc.connect()
flight = conn.space_center.active_vessel.flight()

# [docs]
altitude = conn.add_stream(getattr, flight, "mean_altitude")
altitude.rate = 10
while True:
    print(altitude())
    time.sleep(0.1)
# [/docs]
