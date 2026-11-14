import krpc

conn = krpc.connect(name="Debug Example")
conn.debug.infinite_propellant = True
