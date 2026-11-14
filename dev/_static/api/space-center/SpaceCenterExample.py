import krpc

conn = krpc.connect(name="SpaceCenter Example")
print(conn.space_center.active_vessel.name)
