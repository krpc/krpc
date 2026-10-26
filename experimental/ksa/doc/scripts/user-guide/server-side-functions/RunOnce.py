import krpc

conn = krpc.connect(name="Server side functions")
ksa = conn.ksa
vehicle = ksa.active_vehicle


# [docs]
def lift_off():
    vehicle.auto_pilot.target_pitch_and_heading(90, 90)
    vehicle.auto_pilot.engage()
    vehicle.control.throttle = 1
    vehicle.control.engine_on = True


conn.run(lift_off)
# [/docs]
