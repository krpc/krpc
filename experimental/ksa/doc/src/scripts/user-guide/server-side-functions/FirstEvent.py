import krpc

conn = krpc.connect(name="Server side functions")
ksa = conn.ksa
vehicle = ksa.active_vehicle
orbit = vehicle.orbit
# [docs]
event = conn.add_event(lambda: orbit.apoapsis_altitude > 100_000)
with event.condition:
    event.wait()
print("Apoapsis passed 100 km")
# [/docs]
