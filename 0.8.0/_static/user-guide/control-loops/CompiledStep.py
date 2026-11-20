import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
target_pitch = 45


# [docs]
@conn.compile
def step():
    error = target_pitch - vessel.flight().pitch
    vessel.control.pitch = max(-1, min(1, 0.05 * error))


while True:
    conn.run(step)
# [/docs]
