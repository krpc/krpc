import krpc

conn = krpc.connect()
expression = conn.krpc.Expression
types = conn.krpc.Type

# total = 0; for x in [1, 2, 3]: total += x
total = expression.variable("total", types.int())
x = expression.variable("x", types.int())
values = expression.create_list([expression.constant_int(i) for i in [1, 2, 3]])
program = expression.block_with_variables(
    [total, x],
    [
        expression.assign(total, expression.constant_int(0)),
        expression.for_each(
            x, values, expression.assign(total, expression.add(total, x))
        ),
        total,
    ],
)

print(conn.run_function(program))
