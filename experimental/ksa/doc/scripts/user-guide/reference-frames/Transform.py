import krpc

conn = krpc.connect(name="Transform")
ksa = conn.ksa
vehicle = ksa.active_vehicle
# [docs]
# The direction straight up from the vehicle, in the frame of the body it orbits
up = ksa.transform_direction(
    (0, 0, 1), vehicle.surface_reference_frame, vehicle.orbit.body.reference_frame
)
print(f"Up is {up}")
# [/docs]
