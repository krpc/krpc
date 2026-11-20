local krpc = require 'krpc'
local socket = require 'socket'

local conn = krpc.connect('Docking')
local space_center = conn.space_center
local drawing = conn.drawing
local vessel = space_center.active_vessel
local port = vessel.parts.controlling.docking_port
local target = space_center.target_docking_port
local frame = target.reference_frame

-- Turn to face the target port
print('Turning to face the target')
vessel.control.sas = false
vessel.control.rcs = true
vessel.auto_pilot.reference_frame = frame
vessel.auto_pilot.target_direction = {0, -1, 0}
vessel.auto_pilot.engaged = true
vessel.auto_pilot:wait()

-- Draw the docking axis, and a line from our port to the target
local axis = drawing.add_line({0, 0, 0}, {0, 50, 0}, frame)
axis.color = conn.std_lib.ColorRGB(0, 1, 0)
local approach = drawing.add_line({0, 0, 0}, {0, 0, 0}, frame)
approach.color = conn.std_lib.ColorRGB(1, 1, 0)

-- Approach the target port along its axis
print('Approaching')
local docking = space_center.DockingPortState.docking.value
local docked = space_center.DockingPortState.docked.value
while port.state.value ~= docking and port.state.value ~= docked do
    local p = port:position(frame)
    local x, y, z = p[1], p[2], p[3]
    approach.start = p

    -- Line up on the axis, then close in along it
    local off_axis = math.sqrt(x * x + z * z)
    local goal_y = 0
    if off_axis >= 0.5 then
        goal_y = math.max(y, 5)
    end
    local ex, ey, ez = -x, goal_y - y, -z
    local distance = math.sqrt(ex * ex + ey * ey + ez * ez)
    local speed = math.min(1, math.max(0.1, 0.2 * distance))
    local scale = speed / math.max(distance, 0.01)

    -- Thrust towards the velocity we want, in the vessel's frame
    local v = vessel:velocity(frame)
    local thrust = {
        2 * (ex * scale - v[1]),
        2 * (ey * scale - v[2]),
        2 * (ez * scale - v[3])}
    local t = space_center.transform_direction(
        thrust, frame, vessel.reference_frame)
    vessel.control.right = math.max(-1, math.min(1, t[1]))
    vessel.control.forward = math.max(-1, math.min(1, t[2]))
    vessel.control.up = math.max(-1, math.min(1, -t[3]))
    socket.sleep(0.05)
end

-- Stop the thrusters, and remove the drawing
vessel.control.right = 0
vessel.control.forward = 0
vessel.control.up = 0
vessel.auto_pilot.engaged = false
drawing.clear()
print('Docked')
