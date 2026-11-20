import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
event = conn.add_event(
    lambda: any(not engine.has_fuel for engine in vessel.parts.engines)
)
with event.condition:
    event.wait()
    print("An engine has run out of fuel")
# [/docs]
