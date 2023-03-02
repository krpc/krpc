import krpc

conn = krpc.connect(name="Foo", stream_port=None)
conn.close()

