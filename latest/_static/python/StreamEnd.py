import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
temperature = conn.add_stream(getattr, vessel.parts.root, "temperature")

try:
    with temperature.condition:
        while True:
            temperature.wait()
            print("Temperature", temperature())
except conn.krpc.ObjectDestroyedException:
    print("The part was destroyed")
