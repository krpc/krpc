import krpc

conn = krpc.connect(name="Trajectories Example")
body = conn.space_center.active_vessel.orbit.body
print(conn.trajectories.impact_position(body.reference_frame))
