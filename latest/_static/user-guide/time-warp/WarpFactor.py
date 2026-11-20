import krpc

conn = krpc.connect()
space_center = conn.space_center

# [docs]
print(space_center.maximum_rails_warp_factor)
space_center.rails_warp_factor = 3
# [/docs]
