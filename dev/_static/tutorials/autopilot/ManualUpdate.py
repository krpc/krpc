import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
ap = vessel.auto_pilot


def control_law(pitch: float) -> float:
    return 0.05 * (45 - pitch)


def record(error: float, pitch: float) -> None:
    print(error, pitch)


# [docs]
ap.update_mode = conn.space_center.AutoPilotUpdateMode.manual
while True:
    conn.krpc.hold_tick()
    try:
        ap.target_pitch_and_heading(control_law(vessel.flight().pitch), 90)
        ap.update()
        record(ap.error, vessel.control.pitch)
    finally:
        conn.krpc.release_tick()
# [/docs]
