import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
ap = vessel.auto_pilot
ap.reference_frame = vessel.surface_reference_frame
ap.target_pitch_and_heading(90, 90)  # point straight up, facing east
ap.engaged = True
ap.wait()  # blocks until the vessel has settled on the target
ap.engaged = False
# [/docs]
