import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
stream = conn.add_stream(lambda: vessel.name.upper())
# [/docs]
