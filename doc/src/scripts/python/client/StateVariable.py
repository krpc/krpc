import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight_info = vessel.flight()


# [docs]
def highest_altitude():
    highest = 0.0

    def update() -> float:
        nonlocal highest
        highest = max(highest, flight_info.mean_altitude)
        return highest

    return update


stream = conn.add_stream(highest_altitude())
# [/docs]
