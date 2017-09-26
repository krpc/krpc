import krpc
conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()

# Convert a remote procedure call to a message,
# so it can be passed to the server
mean_altitude = conn.get_call(getattr, flight, 'mean_altitude')

# Create an expression on the server
expr = conn.expressions.greater_than(
    conn.expressions.call(mean_altitude),
    conn.expressions.constant_int(1000))

# Create an event from the expression
event = conn.events.create(expr)

# Wait on the event
with event.condition:
    event.wait()
