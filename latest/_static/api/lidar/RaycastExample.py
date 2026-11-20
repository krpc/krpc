import math

import krpc

conn = krpc.connect(name="Raycast Example")
sc = conn.space_center
part = sc.active_vessel.parts.root
frame = part.reference_frame

# A fan of 8 rays below the part, 10 degrees apart
origin = (0.0, 0.0, 0.0)
directions = []
for i in range(8):
    angle = math.radians(10 * i - 35)
    directions.append((math.sin(angle), -math.cos(angle), 0.0))

# Cast every ray in the same physics tick
distances = conn.add_stream(
    lambda: [sc.raycast_distance(origin, d, frame) for d in directions]
)
print(distances())
