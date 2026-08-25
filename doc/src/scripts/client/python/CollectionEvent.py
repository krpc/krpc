import krpc

conn = krpc.connect()
expression = conn.krpc.Expression
types = conn.krpc.Type
vessel = conn.space_center.active_vessel

# A function taking an engine, returning true if it is out of fuel
engine = expression.parameter("engine", types.class_type("SpaceCenter", "Engine"))
has_fuel = conn.get_call(getattr, vessel.parts.engines[0], "has_fuel")
out_of_fuel = expression.lambda_(
    [engine],
    expression.not_(expression.call_with_arguments(has_fuel, {0: engine})),
)

# Whether any engine satisfies it
engines = conn.get_call(getattr, vessel.parts, "engines")
expr = expression.any(expression.call(engines), out_of_fuel)

event = conn.krpc.add_event(expr)
with event.condition:
    event.wait()
    print("An engine has run out of fuel")
