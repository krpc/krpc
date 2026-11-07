local krpc = require 'krpc'
local conn = krpc.connect()
local flight = conn.space_center.active_vessel:flight()

-- Build a function that computes
-- the vessel's altitude in kilometers
local Expression = conn.krpc.Expression
local mean_altitude = conn:get_call(flight.get_mean_altitude, flight)
local expr = Expression.divide(Expression.call(mean_altitude), Expression.constant_double(1000))

-- Run the function in a loop, as this client has no streams
while true do
  print(string.format('Altitude: %.3f km', conn:run(expr)))
end
