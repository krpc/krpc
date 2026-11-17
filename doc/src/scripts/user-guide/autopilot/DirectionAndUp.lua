local krpc = require 'krpc'
local List = require 'pl.List'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
-- [docs]
local ap = vessel.auto_pilot
ap.reference_frame = vessel.surface_reference_frame
local up = List{0, 1, 0} -- north, in the surface frame, off the flight path

-- straight up at the pad, roof toward north
ap:set_direction_and_up(List{1, 0, 0}, up, 0)
ap.engaged = true
ap:wait()

-- ease over to the eastern horizon; roll stays defined through vertical
for i = 1, 90 do
  local pitch = math.rad(90 - i) -- 89 down to 0 degrees
  local direction = List{math.sin(pitch), 0, math.cos(pitch)}
  ap:set_direction_and_up(direction, up, 0)
  ap:wait()
end
-- [/docs]
