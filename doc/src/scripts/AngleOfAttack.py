import krpc, math, time
conn = krpc.connect(name='Angle of attack')
vessel = conn.space_center.active_vessel

while True:

    d = vessel.direction(vessel.orbit.body.reference_frame)
    v = vessel.velocity(vessel.orbit.body.reference_frame)

    # Compute the dot product of d and v
    dotprod = d[0]*v[0] + d[1]*v[1] + d[2]*v[2]

    # Compute the magnitude of v
    vmag = math.sqrt(v[0]**2 + v[1]**2 + v[2]**2)
    # Note: don't need to magnitude of d as it is a unit vector

    # Compute the angle between the vectors
    if dotprod == 0:
        angle = 0
    else:
        angle = abs(math.acos (dotprod / vmag) * (180. / math.pi))

    print('Angle of attack = %.1f' % angle)

    time.sleep(1)
