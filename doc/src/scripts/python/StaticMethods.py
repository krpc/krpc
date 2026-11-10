import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
Orbit = conn.space_center.Orbit

# Call a static method on the class
print(Orbit.reference_plane_direction(vessel.reference_frame))

# Stream it
direction = conn.add_stream(Orbit.reference_plane_direction, vessel.reference_frame)
print(direction())
