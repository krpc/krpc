import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight = vessel.flight()
expression = conn.krpc.Expression
types = conn.krpc.Type
mean_altitude = conn.get_call(getattr, flight, "mean_altitude")
# [docs]
# Truncate the altitude to a whole number of meters
expr = expression.cast(expression.call(mean_altitude), types.int())
# [/docs]
