local krpc = require 'krpc'
local conn = krpc.connect()
local Expression = conn.krpc.Expression
local resources = conn.space_center.active_vessel.resources
-- [docs]
local amount = conn:get_call(resources.amount, resources, 'LiquidFuel')
local expr = Expression.if_then(
  Expression.less_than(Expression.call(amount), Expression.constant_double(100)),
  Expression.throw('KRPC', 'InvalidOperationException', Expression.constant_string('not enough fuel'))
)
conn:run(expr)
-- [/docs]
