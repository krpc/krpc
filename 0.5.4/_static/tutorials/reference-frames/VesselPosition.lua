local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
print(vessel:position(vessel.orbit.body.reference_frame))
