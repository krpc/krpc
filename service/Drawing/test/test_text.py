import krpc

conn = krpc.connect(address='10.0.2.2')
drawing = conn.drawing
ref = conn.space_center.active_vessel.reference_frame

t = drawing.add_text ("Hello World",ref,(0,0,0),(0,0,0,1))

print t.string
print t.size
print t.character_size
print t.font

while True:
    pass
