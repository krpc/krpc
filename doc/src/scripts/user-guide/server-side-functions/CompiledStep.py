import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
step = conn.compile(lambda: vessel.control.toggle_action_group(1))
while True:
    conn.run(step)
# [/docs]
