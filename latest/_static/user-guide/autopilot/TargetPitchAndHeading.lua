local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
-- [docs]
local ap = vessel.auto_pilot
ap.reference_frame = vessel.surface_reference_frame
-- 80 degrees above the horizon, facing east
ap:target_pitch_and_heading(80, 90)
ap.engaged = true
ap:wait() -- blocks until the vessel has settled on the target
ap.engaged = false
-- [/docs]
