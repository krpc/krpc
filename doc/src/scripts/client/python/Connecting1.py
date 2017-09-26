import krpc
conn = krpc.connect()
print(conn.krpc.get_status().version)
