local krpc = require 'krpc'
local socket = require 'socket'
local conn = krpc.connect('Launch')
local vessel = conn.space_center.active_vessel
local flight = vessel:flight()

vessel.auto_pilot:target_pitch_and_heading(90, 90)
vessel.auto_pilot.engaged = true
vessel.control.throttle = 1
vessel.control:activate_next_stage()
print(vessel.name .. ' has launched')

while flight.mean_altitude < 10000 do
    socket.sleep(0.1)
end

vessel.control.throttle = 0
vessel.auto_pilot.engaged = false
print(vessel.name .. ' reached 10 km')
