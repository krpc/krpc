import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel


# [docs]
def combined_isp():
    engines = [e for e in vessel.parts.engines if e.active and e.has_fuel]
    thrust = sum(engine.available_thrust for engine in engines)
    fuel_consumption = sum(
        engine.available_thrust / engine.specific_impulse
        for engine in engines
    )
    return thrust / fuel_consumption


isp = conn.run(combined_isp)
# [/docs]
print("Combined Isp = %.0f seconds" % isp)
