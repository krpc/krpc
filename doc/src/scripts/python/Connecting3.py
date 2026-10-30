import krpc

with krpc.connect(name="My Example Program") as conn:
    print(conn.krpc.get_status().version)
