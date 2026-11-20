import krpc

conn = krpc.connect()
flight = conn.space_center.active_vessel.flight()

# [docs]
event = conn.add_event(lambda: flight.mean_altitude > 10000)
with event.condition:
    event.wait()
print("Above 10 km")
# [/docs]
