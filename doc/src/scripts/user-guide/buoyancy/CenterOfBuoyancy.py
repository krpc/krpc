import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
flight = vessel.flight(vessel.surface_reference_frame)
center_of_buoyancy = flight.center_of_buoyancy
center_of_mass = flight.center_of_mass
# [/docs]
