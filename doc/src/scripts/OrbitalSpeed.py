import krpc, time
conn = krpc.connect(name='Orbital speed')
vessel = conn.space_center.active_vessel

while True:

    velocity = vessel.flight(vessel.orbit.body.non_rotating_reference_frame).velocity
    print('Orbital velocity = (%.1f, %.1f, %.1f)' % velocity)

    speed = vessel.flight(vessel.orbit.body.non_rotating_reference_frame).speed
    print('Orbital speed = %.1f m/s' % speed)

    time.sleep(1)
