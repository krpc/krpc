import krpc
conn = krpc.connect()
vessel = conn.space_center.active_vessel
print(vessel.position(vessel.orbit.body.reference_frame))
