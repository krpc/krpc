import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel


# [docs]
def burn_ratings():
    ratings: list[str] = []
    for engine in vessel.parts.engines:
        if not engine.active:
            continue
        if engine.thrust > 0.9 * engine.available_thrust:
            ratings.append("full")
        else:
            ratings.append("partial")
    return ratings


print(conn.run(burn_ratings))
# [/docs]
