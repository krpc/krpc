import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()


# [docs]
def altitude_km():
    try:
        return flight.mean_altitude / 1000
    except conn.krpc.ObjectNotLoadedException:
        return 0.0


stream = conn.add_stream(altitude_km)
# [/docs]
