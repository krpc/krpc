import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel


def control_law(pitch: float) -> float:
    return 0.05 * (45 - pitch)


# [docs]
try:
    while True:
        conn.krpc.next_tick()
        pitch = vessel.flight().pitch
        vessel.control.pitch = control_law(pitch)
finally:
    conn.krpc.release_tick()
# [/docs]
