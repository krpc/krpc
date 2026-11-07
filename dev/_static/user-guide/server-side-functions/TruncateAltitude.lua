local krpc = require 'krpc'
local conn = krpc.connect()
local Expression = conn.krpc.Expression
local flight = conn.space_center.active_vessel:flight()
local mean_altitude = conn:get_call(flight.get_mean_altitude, flight)
-- [docs]
-- Truncate the altitude to a whole number of meters
local expr = Expression.cast(Expression.call(mean_altitude), conn.krpc.Type.int())
-- [/docs]
print(conn:run(expr))
