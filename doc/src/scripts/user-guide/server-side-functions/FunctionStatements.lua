local krpc = require 'krpc'
local conn = krpc.connect()
local Expression = conn.krpc.Expression
local Type = conn.krpc.Type

-- total = 0; for x in {1, 2, 3} do total = total + x end
local total = Expression.variable('total', Type.int())
local x = Expression.variable('x', Type.int())
local values = Expression.create_list({
  Expression.constant_int(1),
  Expression.constant_int(2),
  Expression.constant_int(3)
})
local program = Expression.block_with_variables(
  {total, x},
  {
    Expression.assign(total, Expression.constant_int(0)),
    Expression.for_each(x, values, Expression.assign(total, Expression.add(total, x))),
    total
  }
)

print(conn:run(program))
