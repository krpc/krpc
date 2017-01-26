import krpc
conn = krpc.connect()
vessel = conn.space_center.active_vessel

active_engines = [e for e in vessel.parts.engines if e.active and e.has_fuel]

print('Active engines:')
for engine in active_engines:
    print('   %s in stage %d' % (engine.part.title, engine.part.stage))

thrust = sum(engine.thrust for engine in active_engines)
fuel_consumption = sum(engine.thrust / engine.specific_impulse for engine in active_engines)
isp = thrust / fuel_consumption

print('Combined vacuum Isp = %d seconds' % isp)
