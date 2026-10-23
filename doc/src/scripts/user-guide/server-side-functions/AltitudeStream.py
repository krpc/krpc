import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()
# [docs]
# The vessel's altitude, in kilometers, computed on the server
stream = conn.add_stream(lambda: flight.mean_altitude / 1000)
print("Altitude:", stream(), "km")
# [/docs]
