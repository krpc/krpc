import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel


# [docs]
@conn.compile
def check_fuel():
    if vessel.resources.amount("LiquidFuel") < 100:
        raise RuntimeError("not enough fuel")


# [/docs]
