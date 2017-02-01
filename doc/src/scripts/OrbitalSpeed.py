import time
import krpc

conn = krpc.connect(name='Orbital speed')
vessel = conn.space_center.active_vessel
ref_frame = vessel.orbit.body.non_rotating_reference_frame

while True:
    velocity = vessel.flight(ref_frame).velocity
    print('Orbital velocity = (%.1f, %.1f, %.1f)' % velocity)

    speed = vessel.flight(ref_frame).speed
    print('Orbital speed = %.1f m/s' % speed)

    time.sleep(1)
