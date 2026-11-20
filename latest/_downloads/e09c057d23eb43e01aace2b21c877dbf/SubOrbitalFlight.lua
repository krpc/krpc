local krpc = require 'krpc'
local socket = require 'socket'
local conn = krpc.connect('Sub-orbital flight')

local vessel = conn.space_center.active_vessel

vessel.auto_pilot:target_pitch_and_heading(90, 90)
vessel.auto_pilot.engaged = true
vessel.control.throttle = 0.5
socket.sleep(1)

print('Launch!')
vessel.control:activate_next_stage()

while vessel:flight().mean_altitude < 500 do
    socket.sleep(0.1)
end

print('Pitching over')
vessel.auto_pilot.target_smoothing_time = 10
vessel.auto_pilot:target_pitch_and_heading(70, 90)

while vessel.resources:amount('LiquidFuel') > 0.1 do
    socket.sleep(0.1)
end

print('Flame out')
vessel.control.throttle = 0
vessel.auto_pilot.reference_frame = vessel.surface_velocity_reference_frame
vessel.auto_pilot.target_direction = {0, 1, 0}

local body_frame = vessel.orbit.body.reference_frame
while vessel:flight(body_frame).vertical_speed > 0 do
    socket.sleep(0.1)
end

print('Apoapsis reached, separating')
vessel.control:activate_next_stage()
vessel.auto_pilot.target_direction = {0, -1, 0}
vessel.auto_pilot:wait()
vessel.auto_pilot.engaged = false

while vessel:flight().surface_altitude > 2000 do
    socket.sleep(0.1)
end

print('Deploying the parachute')
vessel.parts.parachutes[1].deploy_altitude = 550
vessel.control:activate_next_stage()

local landed = conn.space_center.VesselSituation.landed.value
local splashed = conn.space_center.VesselSituation.splashed.value
while vessel.situation.value ~= landed and
      vessel.situation.value ~= splashed do
    print(string.format('Altitude = %.1f meters',
                        vessel:flight().surface_altitude))
    socket.sleep(1)
end
print('Landed!')
