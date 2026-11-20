import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
frame = vessel.orbit.body.non_rotating_reference_frame

# [docs]
direction = vessel.direction(frame)
prograde = vessel.flight(frame).prograde
angle = conn.std_lib.vector_angle(direction, prograde)
print(conn.std_lib.radians_to_degrees(angle))
# [/docs]
