import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
expression = conn.krpc.Expression
some_vessel = conn.space_center.vessels[0]
# [docs]
target = conn.get_call(getattr, conn.space_center, "target_vessel")
expr = expression.equal(
    expression.call(target),
    expression.constant_object(some_vessel._object_id),
)
event = conn.add_event(expr)
# [/docs]
