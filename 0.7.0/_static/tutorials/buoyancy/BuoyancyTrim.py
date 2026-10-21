import math

import krpc

conn = krpc.connect(name="Buoyancy Trim")
vessel = conn.space_center.active_vessel
body = vessel.orbit.body

if not body.has_ocean:
    raise RuntimeError(body.name + " has no ocean")

flight = vessel.flight(vessel.surface_reference_frame)

# The fully submerged figures, which do not depend on where the vessel is
buoyancy = flight.buoyant_force_at(body.ocean_density, body.surface_gravity)
torque = flight.buoyant_torque_at(body.ocean_density, body.surface_gravity)
weight = vessel.mass * body.surface_gravity

print("Displacement:  %.1f m^3" % flight.displacement)
print("Buoyancy:      %.0f N fully submerged" % buoyancy)
print("Weight:        %.0f N" % weight)
print("Floats:        %s" % (buoyancy > weight))
print("Torque:        %.0f N m fully submerged" % math.hypot(*torque))

# The trim, read in the surface reference frame. Its x axis points up, so the y and z
# axes span the horizontal. A horizontal offset between the two centers tips the hull
# until they line up again.
center_of_buoyancy = flight.center_of_buoyancy

if center_of_buoyancy is None:
    print("Trim:          vessel is out of the water")
else:
    center_of_mass = flight.center_of_mass
    north = center_of_buoyancy[1] - center_of_mass[1]
    east = center_of_buoyancy[2] - center_of_mass[2]
    print("Submerged:     %.0f%%" % (100 * flight.submerged_portion))
    print("Trim:          %.2f m horizontally" % math.hypot(north, east))
    print("Trim north:    %+.2f m" % north)
    print("Trim east:     %+.2f m" % east)
