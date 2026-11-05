local krpc = require 'krpc'
local conn = krpc.connect()
local flight = conn.space_center.active_vessel:flight()
local Expression = conn.krpc.Expression

-- above = mean_altitude > 1000, or false if the vessel is not loaded
local above = Expression.variable('above', conn.krpc.Type.bool())
local mean_altitude = conn:get_call(flight.get_mean_altitude, flight)
local check = Expression.try_catch(
  Expression.assign(above, Expression.greater_than(Expression.call(mean_altitude), Expression.constant_double(1000))),
  'KRPC', 'ObjectNotLoadedException', nil,
  Expression.assign(above, Expression.constant_bool(false))
)
local above_1000m = Expression.block_with_variables({above}, {check, above})

-- Run the function, as this client has no events
if conn:run(above_1000m) then
  print('Altitude reached 1000m')
end
