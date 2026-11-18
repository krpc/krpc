local krpc = require 'krpc'
local Map = require 'pl.Map'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
local flight = vessel:flight()
local control = vessel.control
local target_pitch = 45
-- [docs]
local Expression = conn.krpc.Expression

-- control.pitch = clamp(0.05 * (target_pitch - flight.pitch), -1, 1)
local pitch = conn:get_call(flight.get_pitch, flight)
local pitch_error = Expression.subtract(
  Expression.constant_double(target_pitch), Expression.call(pitch))
local command = Expression.call_with_arguments(conn.std_lib.clamp, Map{
  [0] = Expression.multiply(
    Expression.constant_double(0.05), pitch_error),
  [1] = Expression.constant_double(-1),
  [2] = Expression.constant_double(1)
})
local step = conn:compile(Expression.call_with_arguments(
  conn.space_center.Control.set_pitch, Map{
    [0] = Expression.constant_object(krpc.get_object_id(control)),
    [1] = Expression.cast(command, conn.krpc.Type.float())
  }))

while true do
  conn:run(step)
end
-- [/docs]
