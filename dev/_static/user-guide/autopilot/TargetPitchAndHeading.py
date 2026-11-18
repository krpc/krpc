import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
ap = vessel.auto_pilot
ap.reference_frame = vessel.surface_reference_frame
# 80 degrees above the horizon, facing east
ap.target_pitch_and_heading(80, 90)
ap.engaged = True
ap.wait()  # blocks until the vessel has settled on the target
ap.engaged = False
# [/docs]
