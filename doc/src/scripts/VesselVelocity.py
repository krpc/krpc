import time
import krpc

conn = krpc.connect(name='Orbital speed')
vessel = conn.space_center.active_vessel
ref_frame = conn.space_center.ReferenceFrame.create_hybrid(
    position=vessel.orbit.body.reference_frame,
    rotation=vessel.surface_reference_frame)

while True:
    velocity = vessel.flight(ref_frame).velocity
    print('Surface velocity = (%.1f, %.1f, %.1f)' % velocity)
    time.sleep(1)
