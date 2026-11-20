local krpc = require 'krpc'
local conn = krpc.connect()
local space_center = conn.space_center
local vessel = space_center.active_vessel

-- [docs]
local apoapsis_ut = space_center.ut + vessel.orbit.time_to_apoapsis
space_center.warp_to(apoapsis_ut - 10)
-- [/docs]
