import krpc

conn = krpc.connect()
# [docs]
vessel = conn.space_center.active_vessel
parachute = vessel.parts.with_name("parachuteSingle")[0].parachute
conn.space_center.quickload()
# reads the parachute of the loaded game
print(parachute.state)
# [/docs]
