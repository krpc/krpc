local krpc = require 'krpc'
local socket = require 'socket'
local math = require 'math'

local turn_start_altitude = 250
local turn_end_altitude = 45000
local target_altitude = 150000

local conn = krpc.connect('Launch into orbit')
local vessel = conn.space_center.active_vessel

local flight = vessel:flight()
local stage_5_resources = vessel:decouple_stage_at(5):resources(false)
local stage_3_resources = vessel:decouple_stage_at(3):resources(false)

-- Pre-launch setup
vessel.control.sas = false
vessel.control.rcs = false
vessel.control.throttle = 1

-- Activate the first stage
vessel.auto_pilot.engaged = true
vessel.auto_pilot:target_pitch_and_heading(90, 90)
vessel.control:activate_next_stage()

-- Main ascent loop
local first_stage_separated = false
local second_stage_separated = false
while true do
    -- Gravity turn
    if flight.mean_altitude > turn_start_altitude and
       flight.mean_altitude < turn_end_altitude then
        local frac = (flight.mean_altitude - turn_start_altitude) /
                     (turn_end_altitude - turn_start_altitude)
        local turn_angle = frac * 90
        vessel.auto_pilot:target_pitch_and_heading(90-turn_angle, 90)
    end

    if not first_stage_separated then
        if stage_5_resources:amount('SolidFuel') < 0.1 then
            vessel.control:activate_next_stage()
            vessel.control:activate_next_stage()
            first_stage_separated = true
            print('First stage separated')
        end
    elseif not second_stage_separated then
        if stage_3_resources:amount('LiquidFuel') < 0.1 then
            vessel.control:activate_next_stage()
            vessel.control:activate_next_stage()
            second_stage_separated = true
            print('Second stage separated')
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
local node = vessel.control:add_node(
    conn.space_center.ut + vessel.orbit.time_to_apoapsis, delta_v, 0, 0)

---- Calculate burn time (using rocket equation)
local F = vessel.available_thrust
local Isp = vessel.specific_impulse * 9.82
local m0 = vessel.mass
local m1 = m0 / math.exp(delta_v/Isp)
local flow_rate = F / Isp
local burn_time = (m0 - m1) / flow_rate

-- Orientate ship along the burn
print('Orientating ship for circularization burn')
local frame = vessel.orbit.body.non_rotating_reference_frame
vessel.auto_pilot.reference_frame = frame
vessel.auto_pilot.target_direction = node:remaining_burn_vector(frame)
vessel.auto_pilot:wait()

-- Wait until burn
print('Waiting until circularization burn')
local burn_ut =
    conn.space_center.ut + vessel.orbit.time_to_apoapsis - (burn_time/2.)
local lead_time = 5
conn.space_center.warp_to(burn_ut - lead_time)

-- Execute burn
print('Ready to execute burn')
while vessel.orbit.time_to_apoapsis - (burn_time/2.) > 0 do
end
print('Executing burn')
local lowest_delta_v = node.remaining_delta_v
while node.remaining_delta_v > 0.1 do
    local remaining_delta_v = node.remaining_delta_v
    -- Steer along the remaining burn, which turns as the vessel moves
    -- around its orbit
    vessel.auto_pilot.target_direction = node:remaining_burn_vector(frame)
    -- Full throttle, then throttle down for the last few m/s
    vessel.control.throttle = remaining_delta_v > 5 and 1 or 0.05
    -- Stop if the burn overshoots, as the remaining delta-v then grows
    if remaining_delta_v > lowest_delta_v + 0.1 then
        break
    end
    lowest_delta_v = math.min(lowest_delta_v, remaining_delta_v)
    socket.sleep(0.05)
end
vessel.control.throttle = 0
node:remove()

print('Launch complete')
