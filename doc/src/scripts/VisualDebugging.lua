local krpc = require 'krpc'
local conn = krpc.connect('Visual Debugging')
local vessel = conn.space_center.active_vessel

local ref_frame = vessel.orbit.body.reference_frame
local velocity = vessel:flight(ref_frame).velocity
conn.drawing:add_direction(velocity, ref_frame)

while True do
end
