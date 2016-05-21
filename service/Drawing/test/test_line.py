import krpc

conn = krpc.connect(address='10.0.2.2')
drawing = conn.drawing
ref = conn.space_center.active_vessel.surface_velocity_reference_frame

l = drawing.add_line ((0,0,0),(0,10,0),ref)

while True:
    pass
