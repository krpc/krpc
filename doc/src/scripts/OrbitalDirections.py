import krpc
conn = krpc.connect(name='Orbital directions')
vessel = conn.space_center.active_vessel
ap = vessel.auto_pilot
ap.reference_frame = vessel.orbital_reference_frame
ap.engage()

# Point the vessel in the prograde direction
ap.target_direction = (0,1,0)
ap.wait()

# Point the vessel in the orbit normal direction
ap.target_direction = (0,0,1)
ap.wait()

# Point the vessel in the orbit radial direction
ap.target_direction = (-1,0,0)
ap.wait()

ap.disengage()
