local krpc = require 'krpc'
local conn = krpc.connect()
local Expression = conn.krpc.Expression
local Type = conn.krpc.Type

-- total = 0
-- for x in {1, 2, 3} do
--   if x == 1 or x == 2 then total = total + 10 else total = total + 1 end
-- end
local total = Expression.variable('total', Type.int())
local x = Expression.variable('x', Type.int())
local values = Expression.create_list({
  Expression.constant_int(1),
  Expression.constant_int(2),
  Expression.constant_int(3)
})
local body = Expression.match(
  x,
  {{Expression.constant_int(1), Expression.constant_int(2)}, {}},
  {
    Expression.assign(total, Expression.add(total, Expression.constant_int(10))),
    Expression.assign(total, Expression.add(total, Expression.constant_int(1)))
  }
)
local program = Expression.block_with_variables(
  {total, x},
  {
    Expression.assign(total, Expression.constant_int(0)),
    Expression.for_each(x, values, body),
    total
  }
)

print(conn:run(program))
