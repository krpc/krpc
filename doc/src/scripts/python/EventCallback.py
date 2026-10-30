import time

import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()

event = conn.add_event(lambda: flight.mean_altitude > 1000)


def reached():
    print("Altitude reached 1000m")


event.add_callback(reached)

# Keep the program running, while the callback is called
while True:
    time.sleep(1)
