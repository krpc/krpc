import krpc

conn = krpc.connect(name="Server side functions")
ksa = conn.ksa
vehicle = ksa.active_vehicle
engines = [part.engine for part in vehicle.parts if part.engine is not None]
# [docs]
event = conn.add_event(
    lambda: any(engine.active and not engine.has_propellant for engine in engines)
)
with event.condition:
    event.wait()
print("An engine has run out of propellant")
# [/docs]
