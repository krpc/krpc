import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
body = vessel.orbit.body
flight = vessel.flight()
displacement = flight.displacement
buoyancy = flight.buoyant_force_at(body.ocean_density, body.surface_gravity)
# [/docs]
