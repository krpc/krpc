local krpc = require 'krpc'

local conn = krpc.connect('Trajectories Example')
local body = conn.space_center.active_vessel.orbit.body
print(conn.trajectories.impact_position(body.reference_frame))
