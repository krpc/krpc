local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
local frame = vessel.orbit.body.non_rotating_reference_frame

-- [docs]
local direction = vessel:direction(frame)
local prograde = vessel:flight(frame).prograde
local angle = conn.std_lib.vector_angle(direction, prograde)
print(conn.std_lib.radians_to_degrees(angle))
-- [/docs]
