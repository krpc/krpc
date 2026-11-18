import math
import time

import krpc

conn = krpc.connect(name="Angle of attack")
vessel = conn.space_center.active_vessel

while True:
    d = vessel.direction(vessel.orbit.body.reference_frame)
    v = vessel.velocity(vessel.orbit.body.reference_frame)

    # Compute the dot product of d and v
    dotprod = d[0] * v[0] + d[1] * v[1] + d[2] * v[2]

    # Compute the magnitude of v, as d is a unit vector
    vmag = math.sqrt(v[0] ** 2 + v[1] ** 2 + v[2] ** 2)

    # Compute the angle between the vectors
    angle = 0.0
    if vmag > 0:
        angle = math.acos(dotprod / vmag) * (180.0 / math.pi)

    print("Angle of attack = %.1f degrees" % angle)

    time.sleep(1)
