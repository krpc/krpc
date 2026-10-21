import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
body = vessel.orbit.body
flight = vessel.flight(vessel.surface_reference_frame)
# [docs]
torque = flight.buoyant_torque_at(body.ocean_density, body.surface_gravity)
# [/docs]
