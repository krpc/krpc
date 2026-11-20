local krpc = require 'krpc'
local List = require 'pl.List'
local conn = krpc.connect('Raycast Example')
local sc = conn.space_center
local part = sc.active_vessel.parts.root
local frame = part.reference_frame

-- A fan of 8 rays below the part, 10 degrees apart
local origin = List{0, 0, 0}
for i = 0, 7 do
  local angle = math.rad(10 * i - 35)
  local direction = List{math.sin(angle), -math.cos(angle), 0}
  print(sc.raycast_distance(origin, direction, frame))
end
