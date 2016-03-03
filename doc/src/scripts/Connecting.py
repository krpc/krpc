import krpc
conn = krpc.connect(name='Remote example', address='my.domain.name', rpc_port=1000, stream_port=1001)
print(conn.krpc.get_status().version)
