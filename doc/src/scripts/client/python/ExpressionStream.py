import time
import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()

expression = conn.krpc.Expression

# Create an expression on the server, that computes
# the vessel's altitude in kilometers
expr = expression.divide(
    expression.call(conn.get_call(getattr, flight, "mean_altitude")),
    expression.constant_double(1000),
)

# Stream the value of the expression
stream = conn.add_expression_stream(expr)
while True:
    print("Altitude:", stream(), "km")
    time.sleep(1)
