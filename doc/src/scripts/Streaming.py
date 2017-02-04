import krpc
conn = krpc.connect()
vessel = conn.space_center.active_vessel
refframe = vessel.orbit.body.reference_frame
while True:
    print(vessel.position(refframe))
