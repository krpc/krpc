local krpc = require 'krpc'
local platform = require 'krpc.platform'
local math = require 'math'
local List = require 'pl.List'

local turn_start_altitude = 250
local turn_end_altitude = 45000
local target_altitude = 150000

local conn = krpc.connect('Launch into orbit')
local vessel = conn.space_center.active_vessel

flight = vessel:flight()
stage_2_resources = vessel:resources_in_decouple_stage(2, False)

-- Pre-launch setup
vessel.control.sas = false
vessel.control.rcs = false
vessel.control.throttle = 1

-- Countdown...
print('3...')
platform.sleep(1)
print('2...')
platform.sleep(1)
print('1...')
platform.sleep(1)
print('Launch!')

-- Activate the first stage
vessel.control:activate_next_stage()
vessel.auto_pilot:engage()
vessel.auto_pilot:target_pitch_and_heading(90, 90)

-- Main ascent loop
local srbs_separated = false
local turn_angle = 0
while true do

    -- Gravity turn
    if flight.mean_altitude > turn_start_altitude and flight.mean_altitude < turn_end_altitude then
        frac = (flight.mean_altitude - turn_start_altitude) / (turn_end_altitude - turn_start_altitude)
        new_turn_angle = frac * 90
        if math.abs(new_turn_angle - turn_angle) > 0.5 then
            turn_angle = new_turn_angle
            vessel.auto_pilot:target_pitch_and_heading(90-turn_angle, 90)
        end
    end

    -- Separate SRBs when finished
    if not srbs_separated then
        if stage_2_resources:amount('SolidFuel') < 0.1 then
            vessel.control:activate_next_stage()
            srbs_separated = true
            print('SRBs separated')
        end
    end

    -- Decrease throttle when approaching target apoapsis
    if vessel.orbit.apoapsis_altitude > target_altitude*0.9 then
        print('Approaching target apoapsis')
        break
    end
end

-- Disable engines when target apoapsis is reached
vessel.control.throttle = 0.25
while vessel.orbit.apoapsis_altitude < target_altitude do
end
print('Target apoapsis reached')
vessel.control.throttle = 0

-- Wait until out of atmosphere
print('Coasting out of atmosphere')
while flight.mean_altitude < 70500 do
end

---- Plan circularization burn (using vis-viva equation)
print('Planning circularization burn')
local mu = vessel.orbit.body.gravitational_parameter
local r = vessel.orbit.apoapsis
local a1 = vessel.orbit.semi_major_axis
local a2 = r
local v1 = math.sqrt(mu*((2./r)-(1./a1)))
local v2 = math.sqrt(mu*((2./r)-(1./a2)))
local delta_v = v2 - v1
local node = vessel.control:add_node(conn.space_center.ut + vessel.orbit.time_to_apoapsis, delta_v, 0, 0)

---- Calculate burn time (using rocket equation)
local F = vessel.available_thrust
local Isp = vessel.specific_impulse * 9.82
local m0 = vessel.mass
local m1 = m0 / math.exp(delta_v/Isp)
local flow_rate = F / Isp
local burn_time = (m0 - m1) / flow_rate

-- Orientate ship
print('Orientating ship for circularization burn')
vessel.auto_pilot.reference_frame = node.reference_frame
vessel.auto_pilot.target_direction = List{0, 1, 0}
vessel.auto_pilot:wait()

-- Wait until burn
print('Waiting until circularization burn')
local burn_ut = conn.space_center.ut + vessel.orbit.time_to_apoapsis - (burn_time/2.)
local lead_time = 5
conn.space_center.warp_to(burn_ut - lead_time)

-- Execute burn
print('Ready to execute burn')
while vessel.orbit.time_to_apoapsis - (burn_time/2.) > 0 do
end
print('Executing burn')
vessel.control.throttle = 1
platform.sleep(burn_time - 0.1)
print('Fine tuning')
vessel.control.throttle = 0.05
while node:remaining_burn_vector(node.reference_frame)[2] > 0 do
end
vessel.control.throttle = 0
node:remove()

print('Launch complete')
