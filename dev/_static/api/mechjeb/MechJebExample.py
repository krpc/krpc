import krpc

conn = krpc.connect(name="MechJeb Example")
vessel = conn.space_center.active_vessel
ascent = conn.mech_jeb.ascent_autopilot

ascent.desired_orbit_altitude = 100000
ascent.desired_inclination = 6
ascent.autostage = True
ascent.enabled = True

vessel.control.activate_next_stage()

event = conn.add_event(lambda: not ascent.enabled)
with event.condition:
    event.wait()

print("Launch complete")
