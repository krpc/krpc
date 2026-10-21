import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()
# [docs]
event = conn.add_event(lambda: flight.mean_altitude > 1000)
with event.condition:
    event.wait()
    print("Altitude reached 1000m")
# [/docs]
