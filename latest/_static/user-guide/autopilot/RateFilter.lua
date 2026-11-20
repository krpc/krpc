local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
-- [docs]
local ap = vessel.auto_pilot
-- notch a known 1.4 Hz pitch and yaw bending mode from the start
ap.pitch_yaw_rate_filter_mode = conn.space_center.RateFilterMode.notch
ap.pitch_yaw_oscillation_frequency = 1.4
-- [/docs]
