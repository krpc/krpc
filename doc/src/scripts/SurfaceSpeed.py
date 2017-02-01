import time
import krpc

conn = krpc.connect(name='Surface speed')
vessel = conn.space_center.active_vessel
ref_frame = vessel.orbit.body.reference_frame

while True:
    velocity = vessel.flight(ref_frame).velocity
    print('Surface velocity = (%.1f, %.1f, %.1f)' % velocity)

    speed = vessel.flight(ref_frame).speed
    print('Surface speed = %.1f m/s' % speed)

    time.sleep(1)
