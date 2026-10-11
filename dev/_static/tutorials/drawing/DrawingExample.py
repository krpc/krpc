import time
import krpc

conn = krpc.connect(name="Drawing Example")
space_center = conn.space_center
drawing = conn.drawing
vessel = space_center.active_vessel
kerbin = space_center.bodies["Kerbin"]

# Draw a circular orbit 250 km above Kerbin, as a dashed green path with a label
orbit = space_center.Orbit.create_from_orbital_elements(
    kerbin, kerbin.equatorial_radius + 250000, 0, 0, 0, 0, 0, space_center.ut
)
path = drawing.add_orbit(orbit)
path.color = (0, 1, 0)
path.dashed = True
path.label = "Target orbit"

# Mark the north pole in the map view, with the game's flag icon
site = kerbin.surface_position(90, 0, kerbin.reference_frame)
drawing.add_map_marker(
    site, kerbin.reference_frame, drawing.MapMarkerIcon.flag, "North pole"
)

# Draw a yellow arrow from the vessel, pointing prograde
arrow = drawing.add_direction_from_com((0, 1, 0), vessel.orbital_reference_frame)
arrow.color = (1, 1, 0)
arrow.end_arrow = True

# Label the arrow with the orbital speed of the vessel
while True:
    arrow.label = "%d m/s" % vessel.orbit.speed
    time.sleep(1)
