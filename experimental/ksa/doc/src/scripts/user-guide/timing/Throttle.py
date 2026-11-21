import krpc

conn = krpc.connect(name="Throttle")
ksa = conn.ksa
vehicle = ksa.active_vehicle
# [docs]
thrust = conn.add_stream(getattr, vehicle, "thrust")
vehicle.control.throttle = 1
vehicle.control.engine_on = True
print(f"Thrust straight after: {thrust()} N")
with thrust.condition:
    thrust.wait()
print(f"Thrust a frame later: {thrust()} N")
vehicle.control.engine_on = False
# [/docs]
