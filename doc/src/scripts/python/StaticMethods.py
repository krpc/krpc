import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
frame = vessel.orbit.body.reference_frame
Orbit = conn.space_center.Orbit

# Call a method on an object
print(vessel.position(frame))

# Call a static method on the class
print(Orbit.reference_plane_direction(frame))

# Stream it
direction = conn.add_stream(Orbit.reference_plane_direction, frame)
print(direction())
