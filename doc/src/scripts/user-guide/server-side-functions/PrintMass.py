import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
conn.run(lambda: print("Mass:", vessel.mass, "kg"))
# [/docs]
