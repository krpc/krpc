import krpc

conn = krpc.connect(name="Velocities")
ksa = conn.ksa
vehicle = ksa.active_vehicle
# [docs]
body = vehicle.orbit.body
surface = vehicle.velocity(body.reference_frame)
orbital = vehicle.velocity(body.non_rotating_reference_frame)
print(f"Surface velocity {surface}")
print(f"Orbital velocity {orbital}")
# [/docs]
