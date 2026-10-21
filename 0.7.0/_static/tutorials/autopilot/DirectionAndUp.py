import math

import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
ap = vessel.auto_pilot
ap.reference_frame = vessel.surface_reference_frame
up = (0, 1, 0)  # north, in the surface frame, off the flight path

# straight up at the pad, wings aligned to north
ap.set_direction_and_up((1, 0, 0), up, 0)
ap.engaged = True
ap.wait()

# ease over towards the eastern horizon; roll stays defined through the vertical
for i in range(1, 91):
    pitch = math.radians(90 - i)  # 90° down to 0°
    direction = (math.sin(pitch), 0, math.cos(pitch))
    ap.set_direction_and_up(direction, up, 0)
    ap.wait()
# [/docs]
