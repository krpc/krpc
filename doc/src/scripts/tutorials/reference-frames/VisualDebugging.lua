local krpc = require 'krpc'
local conn = krpc.connect('Visual Debugging')
local vessel = conn.space_center.active_vessel

local ref_frame = vessel.surface_velocity_reference_frame
conn.drawing.add_direction(List{0, 1, 0}, ref_frame)
while true do
end
