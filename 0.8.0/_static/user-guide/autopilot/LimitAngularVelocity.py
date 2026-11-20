import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
ap = vessel.auto_pilot
# limit the angular velocity by max_angular_velocity only
ap.limit_angular_velocity = False
ap.max_angular_velocity = (0.3, 0.5, 0.3)
# [/docs]
