local krpc = require 'krpc'
local Map = require 'pl.Map'
local conn = krpc.connect()
local Expression = conn.krpc.Expression
local parts = conn.space_center.active_vessel.parts

-- A function taking an engine, returning true if it is out of fuel
local engine = Expression.parameter('engine', conn.krpc.Type.class_type('SpaceCenter', 'Engine'))
local first_engine = parts.engines[1]
local has_fuel = conn:get_call(first_engine.get_has_fuel, first_engine)
-- A Lua keyword is not a field name, so not is reached by its string
local out_of_fuel = Expression.lambda(
  {engine},
  Expression['not'](Expression.call_with_arguments(has_fuel, Map{[0] = engine}))
)

-- Whether any engine satisfies it
local engines = conn:get_call(parts.get_engines, parts)
local expr = Expression.any(Expression.call(engines), out_of_fuel)

print('An engine is out of fuel: ' .. tostring(conn:run(expr)))
