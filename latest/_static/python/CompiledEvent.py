import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()

# Create an event that triggers when the lambda returns true
event = conn.add_event(lambda: flight.mean_altitude > 1000)

# Wait on the event
with event.condition:
    event.wait()
    print("Altitude reached 1000m")
