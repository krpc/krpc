local krpc = require 'krpc'
local Map = require 'pl.Map'
local conn = krpc.connect()
local parts = conn.space_center.active_vessel.parts
-- [docs]
local Expression = conn.krpc.Expression

-- for _, parachute in ipairs(parts.parachutes) do
--   parachute:deploy()
-- end
local parachute = Expression.variable(
  'parachute', conn.krpc.Type.class_type('SpaceCenter', 'Parachute'))
local deploy = Expression.call_with_arguments(
  conn.space_center.Parachute.deploy, Map{[0] = parachute})
local parachutes = conn:get_call(parts.get_parachutes, parts)
local program = Expression.block_with_variables(
  {parachute},
  {Expression.for_each(parachute, Expression.call(parachutes), deploy)}
)

conn:run(program)
-- [/docs]
