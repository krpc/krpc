local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
local Orbit = conn.space_center.Orbit

-- Call a static method on the class
print(Orbit.reference_plane_direction(vessel.reference_frame))
