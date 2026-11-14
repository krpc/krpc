import krpc

conn = krpc.connect(name="Hello World")
vessel = conn.space_center.active_vessel
print(vessel.name)
