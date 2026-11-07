import krpc

conn = krpc.connect()
flight = conn.space_center.active_vessel.flight()
expr = conn.krpc.Expression.greater_than(
    conn.krpc.Expression.call(conn.get_call(getattr, flight, "mean_altitude")),
    conn.krpc.Expression.constant_double(1000),
)
with conn.add_event(expr) as event:
    with event.condition:
        event.wait()
        print("Altitude reached 1000m")
# The event was removed at the end of the block
