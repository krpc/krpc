import krpc

conn = krpc.connect()
space_center = conn.space_center
vessel = space_center.active_vessel

# [docs]
apoapsis_ut = space_center.ut + vessel.orbit.time_to_apoapsis
space_center.warp_to(apoapsis_ut - 10)
# [/docs]
