import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel


# [docs]
def heaviest_engine():
    best = None
    for engine in vessel.parts.engines:
        if best is None or engine.part.mass > best.part.mass:
            best = engine
    return best


# [/docs]
