import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
threshold = 50000
event = conn.add_event(lambda: vessel.orbit.apoapsis_altitude > threshold * 2)
# [/docs]
