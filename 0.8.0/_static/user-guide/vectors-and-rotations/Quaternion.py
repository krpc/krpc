import krpc

conn = krpc.connect()
q = conn.space_center.active_vessel.flight().rotation
print("(%.2f, %.2f, %.2f, %.2f)" % q)
