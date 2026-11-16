local krpc = require 'krpc'
local socket = require 'socket'

local conn = krpc.connect('MechJeb Example')
local vessel = conn.space_center.active_vessel
local ascent = conn.mech_jeb.ascent_autopilot

ascent.desired_orbit_altitude = 100000
ascent.desired_inclination = 6
ascent.autostage = true
ascent.enabled = true

vessel.control:activate_next_stage()

while ascent.enabled do
  socket.sleep(1)
end

print('Launch complete')
