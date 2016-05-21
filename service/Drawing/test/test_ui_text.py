import krpc

conn = krpc.connect(address='10.0.2.2')
drawing = conn.drawing
ref = conn.space_center.active_vessel.reference_frame

ui = drawing.add_ui ()
t = ui.add_text("foasdad as af safasd \nsf asf ff \nsf adsf sadf safd \nsf sd sdf o", (20,20))
#t.color = (1,0,0)

while True:
    pass
