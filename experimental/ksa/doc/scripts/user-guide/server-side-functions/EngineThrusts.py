import krpc

conn = krpc.connect(name="Server side functions")
ksa = conn.ksa
vehicle = ksa.active_vehicle
# [docs]
engines = [part.engine for part in vehicle.parts if part.engine is not None]
thrusts = conn.add_stream(lambda: [engine.thrust for engine in engines])
print(f"Engine thrusts {thrusts()}")
# [/docs]
