import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
refframe = vessel.orbit.body.reference_frame
with conn.add_stream(vessel.position, refframe) as position:
    while True:
        print(position())
