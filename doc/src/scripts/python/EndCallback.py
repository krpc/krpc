import time

import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
temperature = conn.add_stream(getattr, vessel.parts.root, "temperature")


def print_temperature(value):
    print("Temperature", value)


def print_ending(error):
    print("The stream ended:", error)


temperature.add_callback(print_temperature)
temperature.add_end_callback(print_ending)

# Keep the program running, while the callbacks are called
while True:
    time.sleep(1)
