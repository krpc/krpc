local krpc = require 'krpc'
local platform = require 'krpc.platform'
local List = require 'pl.List'
local conn = krpc.connect('Drawing Example')
local space_center = conn.space_center
local drawing = conn.drawing
local vessel = space_center.active_vessel
local kerbin = space_center.bodies['Kerbin']

-- Draw a circular orbit 250 km above Kerbin, as a dashed green path with a label
local orbit = space_center.Orbit.create_from_orbital_elements(
  kerbin, kerbin.equatorial_radius + 250000, 0, 0, 0, 0, 0, space_center.ut)
local path = drawing.add_orbit(orbit)
path.color = conn.std_lib.ColorRGB(0, 1, 0)
path.dashed = true
path.label = 'Target orbit'

-- Mark the north pole in the map view, with the game's flag icon
local site = kerbin:surface_position(90, 0, kerbin.reference_frame)
drawing.add_map_marker(
  site, kerbin.reference_frame, drawing.MapMarkerIcon.flag, 'North pole')

-- Draw a yellow arrow from the vessel, pointing prograde
local arrow = drawing.add_direction_from_com(List{0, 1, 0}, vessel.orbital_reference_frame)
arrow.color = conn.std_lib.ColorRGB(1, 1, 0)
arrow.end_arrow = true

-- Label the arrow with the orbital speed of the vessel
while true do
  arrow.label = string.format('%.0f m/s', vessel.orbit.speed)
  platform.sleep(1)
end
