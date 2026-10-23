import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
expression = conn.krpc.Expression
# [docs]
amount = conn.get_call(vessel.resources.amount, "LiquidFuel")
expr = expression.if_then(
    expression.less_than(expression.call(amount), expression.constant_double(100)),
    expression.throw(
        "KRPC",
        "InvalidOperationException",
        expression.constant_string("not enough fuel"),
    ),
)
conn.run(expr)
# [/docs]
