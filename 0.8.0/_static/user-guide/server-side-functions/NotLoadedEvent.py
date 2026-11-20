import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()


def above_1000m() -> bool:
    try:
        return flight.mean_altitude > 1000
    except conn.krpc.ObjectNotLoadedException:
        return False


event = conn.add_event(above_1000m)
with event.condition:
    event.wait()
    print("Altitude reached 1000m")
