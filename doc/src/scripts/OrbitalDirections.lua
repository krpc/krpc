local krpc = require 'krpc'
local List = require 'pl.List'
local conn = krpc.connect('Orbital directions')
local vessel = conn.space_center.active_vessel
local ap = vessel.auto_pilot
ap.reference_frame = vessel.orbital_reference_frame
ap:engage()

-- Point the vessel in the prograde direction
ap.target_direction = List{0, 1, 0}
ap:wait()

-- Point the vessel in the orbit normal direction
ap.target_direction = List{0, 0, 1}
ap:wait()

-- Point the vessel in the orbit radial direction
ap.target_direction = List{-1, 0, 0}
ap:wait()

ap:disengage()
