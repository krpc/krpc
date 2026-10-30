import threading

import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel
flight_info = vessel.flight(vessel.orbit.body.reference_frame)


def print_altitude():
    for _ in range(10):
        print("Altitude:", flight_info.mean_altitude)


def print_speed():
    for _ in range(10):
        print("Speed:", flight_info.speed)


# Both threads make calls over the same connection
threads = [
    threading.Thread(target=print_altitude),
    threading.Thread(target=print_speed),
]
for thread in threads:
    thread.start()
for thread in threads:
    thread.join()
