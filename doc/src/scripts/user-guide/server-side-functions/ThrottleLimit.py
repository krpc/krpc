import time

import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
control = vessel.control
flight = vessel.flight(vessel.orbit.body.reference_frame)


# [docs]
@conn.compile
def limit_throttle():
    # Throttle back as the vertical speed nears 100 m/s
    control.throttle = max(0, min(1, (100 - flight.vertical_speed) / 20))


with conn.stream(limit_throttle):
    time.sleep(60)
# [/docs]
