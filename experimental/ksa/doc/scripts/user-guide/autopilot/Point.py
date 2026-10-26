import krpc

conn = krpc.connect(name="Point")
ksa = conn.ksa
vehicle = ksa.active_vehicle
# [docs]
auto_pilot = vehicle.auto_pilot
auto_pilot.target_pitch_and_heading(30, 90)
auto_pilot.engage()
auto_pilot.wait()
print(f"Pointing {auto_pilot.error:.1f} degrees from the target")
auto_pilot.mode = ksa.AutoPilotMode.prograde
auto_pilot.wait()
auto_pilot.disengage()
# [/docs]
