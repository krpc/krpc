import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
engines = vessel.parts.engines
stream = conn.add_stream(lambda: [engine.thrust for engine in engines])
for thrust in stream():
    print(thrust)
# [/docs]
