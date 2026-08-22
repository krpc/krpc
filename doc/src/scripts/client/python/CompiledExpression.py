import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()

# Create an event from a lambda. The condition is compiled into a server
# side expression, and checked on the server each physics tick.
event = conn.add_event(lambda: flight.mean_altitude > 1000)
with event.condition:
    event.wait()
    print("Altitude reached 1000m")

# Remote procedure calls made by the function are re-invoked on each
# evaluation, including calls made on each element of a collection. This
# event fires when any engine on the vessel runs out of fuel:
event = conn.add_event(
    lambda: any(not engine.has_fuel for engine in vessel.parts.engines)
)
with event.condition:
    event.wait()
    print("An engine has run out of fuel")

# Computed values can be streamed by passing a function to
# add_expression_stream. This streams the vessel's altitude in kilometers,
# computed on the server:
stream = conn.add_expression_stream(lambda: flight.mean_altitude / 1000)
print("Altitude:", stream(), "km")
