import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight(vessel.orbit.body.reference_frame)

expression = conn.krpc.Expression

# Convert the remote procedure calls to messages,
# so they can be passed to the server
mean_altitude_call = conn.get_call(getattr, flight, "mean_altitude")
speed_call = conn.get_call(getattr, flight, "speed")

# Build a function that reads the altitude and the speed
expr = expression.create_tuple(
    [expression.call(mean_altitude_call), expression.call(speed_call)]
)

# Run the function once, so both values come from the same physics tick
altitude, speed = conn.run(expr)
print("Altitude:", altitude, "m, speed:", speed, "m/s")
