local krpc = require 'krpc'
local conn = krpc.connect()
local Expression = conn.krpc.Expression
local some_vessel = conn.space_center.vessels[1]
-- [docs]
local target = conn:get_call(conn.space_center.get_target_vessel, conn.space_center)
local expr = Expression.equal(
  Expression.call(target),
  Expression.constant_object(krpc.get_object_id(some_vessel))
)
local is_target = conn:run(expr)
-- [/docs]
print(is_target)
