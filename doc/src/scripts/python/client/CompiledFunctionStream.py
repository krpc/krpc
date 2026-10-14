import time

import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()

# Stream the vessel's altitude in kilometers, computed on the server
stream = conn.add_stream(lambda: flight.mean_altitude / 1000)
while True:
    print("Altitude:", stream(), "km")
    time.sleep(1)
