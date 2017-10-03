import krpc
conn = krpc.connect()
v = conn.space_center.active_vessel.flight().prograde
print(v[0], v[1], v[2])
