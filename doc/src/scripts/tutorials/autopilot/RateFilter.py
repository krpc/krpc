import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
ap = vessel.auto_pilot
# treat the craft as flexible from the start, notching a 1.4 Hz bending mode
ap.pitch_yaw_rate_filter_mode = conn.space_center.RateFilterMode.notch
ap.pitch_yaw_oscillation_frequency = 1.4
# [/docs]
