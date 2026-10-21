import krpc

conn = krpc.connect(name="Visual Debugging")
vessel = conn.space_center.active_vessel

# Draw the axes of the surface reference frame
conn.drawing.add_axes(vessel.surface_reference_frame)

# Draw the direction of the surface velocity, as a labeled arrow
ref_frame = vessel.surface_velocity_reference_frame
line = conn.drawing.add_direction_from_com((0, 1, 0), ref_frame)
line.end_arrow = True
line.label = "Surface velocity"
while True:
    pass
