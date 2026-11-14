import krpc

conn = krpc.connect(name="KRPC Example")
print(conn.krpc.get_status().version)
