local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
local flight = vessel:flight(vessel.orbit.body.reference_frame)

-- Get the remote procedure calls as messages, so they can be passed to the server
local mean_altitude_call = conn:get_call(flight.get_mean_altitude, flight)
local speed_call = conn:get_call(flight.get_speed, flight)

-- Build a function that reads the altitude and the speed
local Expression = conn.krpc.Expression
local expr = Expression.create_tuple({
  Expression.call(mean_altitude_call),
  Expression.call(speed_call)
})

-- Run the function once, so both values come from the same physics tick
local value = conn:run(expr)
print(string.format('Altitude: %.1f m, speed: %.1f m/s', value[1], value[2]))
