import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
for part in vessel.parts.all:
    if part.static_pressure + part.dynamic_pressure > part.max_pressure:
        print(part.title, "is over its pressure limit")
# [/docs]
