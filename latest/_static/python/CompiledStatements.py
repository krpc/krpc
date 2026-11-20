import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel


def shut_down_empty_engines() -> int:
    count = 0
    for engine in vessel.parts.engines:
        if engine.active and not engine.has_fuel:
            engine.active = False
            count += 1
    return count


# The function runs on the server, within a single physics tick
print(conn.run(shut_down_empty_engines), "engines shut down")
