local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
local frame = vessel.orbit.body.reference_frame
local Orbit = conn.space_center.Orbit

-- Call a method on an object
print(vessel:position(frame))

-- Call a static method on the class
print(Orbit.reference_plane_direction(frame))
