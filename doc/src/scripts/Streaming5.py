import krpc
conn = krpc.connect()
with conn.stream(getattr(conn.space_center, 'ut')) as ut:
    print('Universal Time =', ut())
