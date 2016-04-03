with conn.stream(getattr(conn.space_center, 'ut')) as ut:
    print('Universal Time =', ut())
