import krpc

conn = krpc.connect(address='10.0.2.2')
drawing = conn.drawing
ref = conn.space_center.active_vessel.surface_velocity_reference_frame

vertices = [
    (0,0,0),
    (0,10,0),
    (-2,10, 0),
    ( 0,15, 0),
    ( 2,10, 0),
    (0,10,0)
]

l = drawing.add_polygon (vertices,ref)
l.color = (0,1,0)

print l.vertices
print l.thickness
print l.color

while True:
    pass
