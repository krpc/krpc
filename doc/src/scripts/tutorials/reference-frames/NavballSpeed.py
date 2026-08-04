import time
import krpc

conn = krpc.connect(name="Navball speed")
vessel = conn.space_center.active_vessel
obt_frame = vessel.orbit_speed_reference_frame
srf_frame = vessel.surface_speed_reference_frame

while True:
    obt_speed = vessel.flight(obt_frame).speed
    srf_speed = vessel.flight(srf_frame).speed
    print("Orbital speed = %.1f m/s, Surface speed = %.1f m/s" % (obt_speed, srf_speed))
    time.sleep(1)
