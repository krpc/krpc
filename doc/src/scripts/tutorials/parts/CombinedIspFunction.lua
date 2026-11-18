local krpc = require 'krpc'
local Map = require 'pl.Map'
local conn = krpc.connect()
local parts = conn.space_center.active_vessel.parts
-- [docs]
local Expression = conn.krpc.Expression
local Engine = conn.space_center.Engine
local engine = Expression.parameter(
  'engine', conn.krpc.Type.class_type('SpaceCenter', 'Engine'))
-- A property of the engine parameter
local function get(property)
  return Expression.call_with_arguments(property, Map{[0] = engine})
end
-- A function of the engine parameter
local function fn(body)
  return Expression.lambda({engine}, body)
end

-- The active engines with fuel
local engines = Expression.where(
  Expression.call(conn:get_call(parts.get_engines, parts)),
  fn(Expression.conditional_and(
    get(Engine.get_active), get(Engine.get_has_fuel))))
local thrust = get(Engine.get_available_thrust)
local isp = get(Engine.get_specific_impulse)
local total_thrust = Expression.sum(Expression.select(engines, fn(thrust)))
local fuel_consumption = Expression.sum(
  Expression.select(engines, fn(Expression.divide(thrust, isp))))

local combined_isp =
  conn:run(Expression.divide(total_thrust, fuel_consumption))
-- [/docs]
print(string.format('Combined Isp = %.0f seconds', combined_isp))
