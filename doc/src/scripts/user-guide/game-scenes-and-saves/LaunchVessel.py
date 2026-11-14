import krpc

conn = krpc.connect()
space_center = conn.space_center

# [docs]
print(space_center.launchable_vessels("VAB"))
space_center.launch_vessel_from_vab("Kerbal X")
print(space_center.active_vessel.name)
# [/docs]
