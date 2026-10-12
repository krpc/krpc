import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight(vessel.orbit.body.reference_frame)

# Run the lambda on the server, so both values come from the same physics tick
altitude, speed = conn.run(lambda: (flight.mean_altitude, flight.speed))
print("Altitude:", altitude, "m, speed:", speed, "m/s")
