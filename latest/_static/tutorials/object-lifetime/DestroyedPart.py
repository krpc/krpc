import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
part = vessel.parts.all[0]
# [docs]
try:
    print(part.temperature)
except conn.krpc.ObjectDestroyedException:
    print("that part is gone")
# [/docs]
