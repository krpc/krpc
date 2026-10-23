local krpc = require 'krpc'
local List = require 'pl.List'
local conn = krpc.connect('Visual Debugging')
local vessel = conn.space_center.active_vessel

-- Draw the axes of the surface reference frame
conn.drawing.add_axes(vessel.surface_reference_frame)

-- Draw the direction of the surface velocity, as a labeled arrow
local ref_frame = vessel.surface_velocity_reference_frame
local line = conn.drawing.add_direction_from_com(List{0, 1, 0}, ref_frame)
line.end_arrow = true
line.label = 'Surface velocity'
while true do
end
