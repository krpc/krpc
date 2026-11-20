local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
local position = vessel:position(vessel.orbit.body.reference_frame)
print(string.format('(%.1f, %.1f, %.1f)',
                    position[1], position[2], position[3]))
