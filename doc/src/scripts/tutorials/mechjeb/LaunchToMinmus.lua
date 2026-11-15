local krpc = require 'krpc'
local socket = require 'socket'

local conn = krpc.connect('Launch to Minmus')
local sc = conn.space_center
local vessel = sc.active_vessel
local mj = conn.mech_jeb

-- Wait until the condition is true
local function wait_for(condition)
    while not condition() do
        socket.sleep(0.1)
    end
end

-- Wait for the autopilot to disengage, printing each of its steps
local function wait_for_steps(autopilot)
    local last = ''
    while autopilot.enabled do
        -- The step, without the figures that change on each update
        local line = autopilot.status:match('^[^\n]*')
        local step = line:gsub('∞.*', ''):match('^[^%d]*')
        step = step:gsub('[ :=]*$', '')
        if step ~= '' and step ~= last then
            last = step
            print(step)
        end
        socket.sleep(0.1)
    end
end

local function print_orbit()
    local orbit = vessel.orbit
    print(string.format('Orbit of %s, %.0f km by %.0f km',
                        orbit.body.name,
                        orbit.apoapsis_altitude / 1000,
                        orbit.periapsis_altitude / 1000))
end

-- The target orbit, set as in the Ascent Guidance window
local ascent = mj.ascent_guidance
ascent.ascent_type = mj.AscentType.classic
ascent.desired_orbit_altitude = 100000
ascent.desired_inclination = 0

-- The Ascent Settings window
local settings = ascent.settings
settings.autostage = true
settings.limit_angle_of_attack = true
settings.max_angle_of_attack = 5
settings.autowarp = true

ascent.enabled = true
vessel.control:activate_next_stage()

-- Wait for the autopilot to disengage, once the orbit is circular
wait_for_steps(ascent)
print_orbit()

local planner = mj.maneuver_planner
planner.autowarp = true

-- Stage once a stage burns out, during the burns too
mj.utilities.staging.enabled = true

-- Execute the vessel's maneuver nodes, and wait for the burns
local function execute_nodes()
    planner:execute_all_nodes()
    wait_for(function() return not planner.executing_node end)
end

-- Target Minmus, and wait for MechJeb to see the target
local minmus = sc.bodies['Minmus']
sc.target_body = minmus
wait_for(function() return mj.target_controller.normal_target_exists end)

-- Match planes with Minmus, at the nearest relative node
local plane = planner.operation_plane
plane.time_selector.time_reference = mj.TimeReference.rel_nearest_ad
plane:make_nodes()
execute_nodes()

-- The Hohmann transfer, to an encounter with no capture burn
local transfer = planner.operation_transfer
transfer.time_selector.time_reference = mj.TimeReference.computed
transfer.capture = false
transfer:make_nodes()
execute_nodes()

-- Fine tune the encounter, for a periapsis 20 km above Minmus
local correction = planner.operation_course_correction
correction.final_periapsis_altitude = 20000
correction:make_nodes()
execute_nodes()

-- Warp into Minmus' sphere of influence
sc.warp_to(sc.ut + vessel.orbit.time_to_soi_change + 60)

-- Circularize at the periapsis
local circularize = planner.operation_circularize
circularize.time_selector.time_reference = mj.TimeReference.periapsis
circularize:make_nodes()
execute_nodes()

sc.clear_target()
print_orbit()

-- Stage until the next stage would drop the landing legs. This drops
-- the stages below the lander, and activates the lander's engine.
mj.utilities.staging.enabled = false
local legs = vessel.parts.legs[1].part
while vessel.control.current_stage > legs.decouple_stage + 1 do
    vessel.control:activate_next_stage()
end

-- Limit the lander's thrust, so its lowest throttle leaves it falling
for _, engine in ipairs(vessel.parts.engines) do
    engine.thrust_limit = 0.3
end

-- Land on Minmus' Great Flats, and wait for MechJeb to see the target
local landing = mj.landing_guidance
landing:set_target(minmus, 0, -6)
wait_for(function() return mj.target_controller.position_target_exists end)

-- The settings of the Landing Guidance window
landing.touchdown_speed = 1
landing.deploy_gears = true
landing.autowarp = true

landing:land_at_target()

-- Wait for the autopilot to disengage, once the vessel has landed
wait_for_steps(landing)

sc.clear_target()
local flight = vessel:flight()
print(string.format('Landed at latitude %.2f, longitude %.2f',
                    flight.latitude, flight.longitude))
