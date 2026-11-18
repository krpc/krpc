local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
-- [docs]
local ap = vessel.auto_pilot
ap.reference_frame = vessel.surface_reference_frame
ap:target_pitch_and_heading(90, 90) -- start pointing straight up
ap.engaged = true
ap:wait()

-- ease a 10 degree pitch-over, 90 to 80, in over 20 seconds
ap.target_smoothing_time = 20
ap:target_pitch_and_heading(80, 90)
ap:wait() -- returns once the 80 degree target is reached
-- [/docs]
