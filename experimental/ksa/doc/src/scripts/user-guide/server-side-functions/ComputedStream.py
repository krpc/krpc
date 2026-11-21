import krpc

conn = krpc.connect(name="Server side functions")
ksa = conn.ksa
vehicle = ksa.active_vehicle
# [docs]
gravity = vehicle.orbit.body.surface_gravity
ratio = conn.add_stream(lambda: vehicle.thrust / (vehicle.mass * gravity))
print(f"Thrust to weight ratio {ratio():.2f}")
# [/docs]
