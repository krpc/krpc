import krpc
conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()

# Convert a remote procedure call to a message,
# so it can be passed to the server
mean_altitude = conn.get_call(getattr, flight, 'mean_altitude')

# Create an expression on the server
expr = conn.krpc.Expression.greater_than(
    conn.krpc.Expression.call(mean_altitude),
    conn.krpc.Expression.constant_double(1000))

# Create an event from the expression
event = conn.krpc.add_event(expr)

# Wait on the event
with event.condition:
    event.wait()
    print 'Altitude reached 1000m'
