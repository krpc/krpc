import krpc
conn = krpc.connect()
vessel = conn.space_center.active_vessel
with conn.stream(getattr, vessel.control, 'abort') as abort:
    abort.condition.acquire()
    while not abort():
        abort.wait()
    abort.condition.release()
