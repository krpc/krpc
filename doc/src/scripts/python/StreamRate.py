import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight_info = vessel.flight()
altitude = conn.add_stream(getattr, flight_info, "mean_altitude")
# Update the stream at most 10 times a second
altitude.rate = 10
