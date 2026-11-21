import krpc

conn = krpc.connect(name="Server side functions")
ksa = conn.ksa
vehicle = ksa.active_vehicle
# [docs]
threshold = 50_000
event = conn.add_event(lambda: vehicle.orbit.apoapsis_altitude > threshold * 2)
# [/docs]
with event.condition:
    event.wait()
