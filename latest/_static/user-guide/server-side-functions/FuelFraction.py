import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel


# [docs]
def fuel_fraction():
    resources = vessel.resources
    amount = resources.amount("LiquidFuel")
    capacity = resources.max("LiquidFuel")
    return amount / capacity


stream = conn.add_stream(fuel_fraction)
# [/docs]
