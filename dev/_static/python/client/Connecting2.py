import krpc

conn = krpc.connect(name="My Example Program", address="192.168.1.10", port=1000)
print(conn.krpc.get_status().version)
