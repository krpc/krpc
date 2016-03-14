import krpc
conn = krpc.connect(name='Example')
print(conn.krpc.get_status().version)
