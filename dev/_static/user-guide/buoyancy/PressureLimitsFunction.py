import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
# [docs]
over_limit = conn.run(
    lambda: [
        part.title
        for part in vessel.parts.all
        if part.static_pressure + part.dynamic_pressure > part.max_pressure
    ]
)
# [/docs]
