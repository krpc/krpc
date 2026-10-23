import math

import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel


# [docs]
@conn.compile
def burn_time(delta_v: float) -> float:
    exhaust_velocity = vessel.specific_impulse * 9.82
    final_mass = vessel.mass / math.exp(delta_v / exhaust_velocity)
    return (vessel.mass - final_mass) * exhaust_velocity / vessel.available_thrust


for delta_v in (100.0, 250.0, 400.0):
    print(conn.run(burn_time, delta_v))
# [/docs]
