local krpc = require 'krpc'
local conn = krpc.connect()
local Expression = conn.krpc.Expression

-- Build a function that doubles its parameter
local x = Expression.parameter('x', conn.krpc.Type.double())
local func = conn:compile(Expression.lambda({x}, Expression.multiply(x, Expression.constant_double(2))))

print('Result: ' .. conn:run(func, 21))
