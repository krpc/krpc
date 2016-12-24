import krpc
conn = krpc.connect()
vessel = conn.space_center.active_vessel
abort = conn.add_stream(getattr, vessel.control, 'abort')
while not abort():
    pass
