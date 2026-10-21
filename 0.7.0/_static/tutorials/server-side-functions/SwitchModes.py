import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel


# [docs]
def switch_modes() -> str:
    try:
        for engine in vessel.parts.engines:
            engine.toggle_mode()
        return "switched"
    except Exception as error:
        return "could not switch: " + error


# [/docs]
