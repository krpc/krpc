import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
stdlib = conn.std_lib
frame = vessel.orbit.body.reference_frame
stream = conn.add_stream(
    lambda: stdlib.radians_to_degrees(
        stdlib.vector_angle(vessel.direction(frame), vessel.velocity(frame))
    )
)
# [/docs]
