local krpc = require 'krpc'
local List = require 'pl.List'
local conn = krpc.connect('Surface prograde')
local vessel = conn.space_center.active_vessel
local ap = vessel.auto_pilot

ap.reference_frame = vessel.surface_velocity_reference_frame
ap.target_direction = List{0, 1, 0}
ap:engage()
ap:wait()
ap:disengage()
