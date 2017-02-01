local krpc = require 'krpc'
local List = require 'pl.List'
local conn = krpc.connect('Navball directions')
local vessel = conn.space_center.active_vessel
local ap = vessel.auto_pilot
ap.reference_frame = vessel.surface_reference_frame
ap:engage()

-- Point the vessel north on the navball, with a pitch of 0 degrees
ap.target_direction = List{0, 1, 0}
ap:wait()

-- Point the vessel vertically upwards on the navball
ap.target_direction = List{1, 0, 0}
ap:wait()

-- Point the vessel west (heading of 270 degrees), with a pitch of 0 degrees
ap.target_direction = List{0, 0, -1}
ap:wait()

ap:disengage()
