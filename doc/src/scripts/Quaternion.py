import krpc
conn = krpc.connect()
q = conn.space_center.active_vessel.flight().rotation
print(q[0], q[1], q[2], q[3])
