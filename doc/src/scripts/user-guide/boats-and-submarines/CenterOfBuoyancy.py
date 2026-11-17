import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
flight = vessel.flight(vessel.surface_reference_frame)
center_of_buoyancy = flight.center_of_buoyancy
center_of_mass = flight.center_of_mass
# [/docs]
if center_of_buoyancy is not None:
    north = center_of_buoyancy[1] - center_of_mass[1]
    east = center_of_buoyancy[2] - center_of_mass[2]
    print("Trim north = %.2f m, east = %.2f m" % (north, east))
