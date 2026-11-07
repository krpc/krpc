local krpc = require 'krpc'
local conn = krpc.connect()
local flight = conn.space_center.active_vessel:flight()

-- Get the remote procedure call as a message,
-- so it can be passed to the server
local mean_altitude = conn:get_call(flight.get_mean_altitude, flight)

-- Build the function on the server
local Expression = conn.krpc.Expression
local expr = Expression.greater_than(Expression.call(mean_altitude), Expression.constant_double(1000))

-- Run the function once
print('Above 1000m: ' .. tostring(conn:run(expr)))
