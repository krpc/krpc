import krpc

conn = krpc.connect_local(name="My Example Program")
print(conn.krpc.get_status().version)
