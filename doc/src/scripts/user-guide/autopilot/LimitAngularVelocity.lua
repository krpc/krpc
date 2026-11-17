local krpc = require 'krpc'
local List = require 'pl.List'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
-- [docs]
local ap = vessel.auto_pilot
-- limit the angular velocity by max_angular_velocity only
ap.limit_angular_velocity = false
ap.max_angular_velocity = List{0.3, 0.5, 0.3}
-- [/docs]
