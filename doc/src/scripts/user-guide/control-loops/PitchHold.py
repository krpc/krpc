# [docs]
import krpc

conn = krpc.connect(name="pitch hold")
vessel = conn.space_center.active_vessel
target_pitch = 45

try:
    for _ in range(500):
        conn.krpc.next_tick()
        flight = vessel.flight()
        error = target_pitch - flight.pitch
        vessel.control.pitch = max(-1, min(1, 0.05 * error))
finally:
    conn.krpc.release_tick()
    vessel.control.pitch = 0
# [/docs]
