import krpc
conn = krpc.connect(name='Visual Debugging')
vessel = conn.space_center.active_vessel

ref_frame = vessel.orbit.body.reference_frame
velocity = vessel.flight(ref_frame).velocity
conn.drawing.add_direction(velocity, ref_frame)

while True:
    pass
