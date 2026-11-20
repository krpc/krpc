import krpc

conn = krpc.connect()
v = conn.space_center.active_vessel.flight().prograde
print("(%.2f, %.2f, %.2f)" % v)
