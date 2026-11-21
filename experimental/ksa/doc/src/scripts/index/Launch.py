import krpc

conn = krpc.connect(name="Launch")
ksa = conn.ksa
vehicle = ksa.launch_vehicle("Rocket", "CCSFS LC-39A")
flight = vehicle.flight

vehicle.auto_pilot.target_pitch_and_heading(90, 90)
vehicle.auto_pilot.engage()
vehicle.control.throttle = 1
vehicle.control.activate_next_sequence()
vehicle.control.engine_on = True
print(f"{vehicle.name} has launched")

event = conn.add_event(lambda: flight.mean_altitude > 10000)
with event.condition:
    event.wait()

vehicle.control.engine_on = False
vehicle.auto_pilot.disengage()
print(f"{vehicle.name} reached 10 km")
