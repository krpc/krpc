import krpc
conn = krpc.connect()
vessel = conn.space_center.active_vessel
with conn.stream(getattr, vessel.control, 'abort') as abort:
    with abort.condition:
        while not abort():
            abort.wait()
