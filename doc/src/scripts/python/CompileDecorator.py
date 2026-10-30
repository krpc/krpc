import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()


# [docs]
@conn.compile
def altitude_km():
    return flight.mean_altitude / 1000


stream = conn.add_stream(altitude_km)
# [/docs]
