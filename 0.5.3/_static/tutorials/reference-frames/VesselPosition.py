import krpc
conn = krpc.connect()
vessel = conn.space_center.active_vessel
print('(%.1f, %.1f, %.1f)' % vessel.position(vessel.orbit.body.reference_frame))
