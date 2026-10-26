import krpc

conn = krpc.connect(name="Server side functions")
ksa = conn.ksa
vehicle = ksa.active_vehicle


# [docs]
def propellant_fraction():
    amount = 0.0
    capacity = 0.0
    for resource in vehicle.resources:
        amount += resource.amount
        capacity += resource.max
    return amount / capacity


fraction = conn.add_stream(propellant_fraction)
print(f"{fraction():.1%} of the propellant is left")
# [/docs]
