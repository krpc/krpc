import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel


# [docs]
def total_thrust():
    total: float = 0
    for engine in vessel.parts.engines:
        total += engine.thrust
    return total


print(conn.run(total_thrust))
# [/docs]
