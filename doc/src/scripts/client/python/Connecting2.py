import krpc
conn = krpc.connect(
    name='My Example Program',
    address='192.168.1.10',
    rpc_port=1000, stream_port=1001)
print(conn.krpc.get_status().version)
