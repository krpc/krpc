import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()


# [docs]
@conn.compile
def altitude_above(height: float, scale: float = 1000) -> float:
    return (flight.mean_altitude - height) / scale


print(conn.run(altitude_above, 70000))
print(conn.run(altitude_above, 70000, scale=1))
# [/docs]
