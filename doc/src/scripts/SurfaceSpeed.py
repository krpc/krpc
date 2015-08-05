import krpc, time
conn = krpc.connect(name='Surface speed')
vessel = conn.space_center.active_vessel

while True:

    velocity = vessel.flight(vessel.orbit.body.reference_frame).velocity
    print('Surface velocity = (%.1f, %.1f, %.1f)' % velocity)

    speed = vessel.flight(vessel.orbit.body.reference_frame).speed
    print('Surface speed = %.1f m/s' % speed)

    time.sleep(1)
