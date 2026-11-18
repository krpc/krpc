local krpc = require 'krpc'
local Map = require 'pl.Map'
local conn = krpc.connect()
local parts = conn.space_center.active_vessel.parts
-- [docs]
local Expression = conn.krpc.Expression
local Part = conn.space_center.Part
local part = Expression.parameter(
  'part', conn.krpc.Type.class_type('SpaceCenter', 'Part'))
-- A property of the part parameter
local function get(property)
  return Expression.call_with_arguments(property, Map{[0] = part})
end
-- A function of the part parameter
local function fn(body)
  return Expression.lambda({part}, body)
end

local over = Expression.greater_than(
  Expression.add(
    get(Part.get_static_pressure), get(Part.get_dynamic_pressure)),
  get(Part.get_max_pressure))
local titles = Expression.select(
  Expression.where(
    Expression.call(conn:get_call(parts.get_all, parts)), fn(over)),
  fn(get(Part.get_title)))
local over_limit = conn:run(Expression.to_list(titles))
-- [/docs]
for _, title in ipairs(over_limit) do
  print(title)
end
