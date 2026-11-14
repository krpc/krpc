import time

import krpc

conn = krpc.connect(name="Drawing Example")
vessel = conn.space_center.active_vessel
conn.drawing.add_direction_from_com((0, 1, 0), vessel.reference_frame)
time.sleep(10)
