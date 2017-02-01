local krpc = require 'krpc'
local platform = require 'krpc.platform'
local math = require 'math'
local List = require 'pl.List'
local conn = krpc.connect('Pitch/Heading/Roll')
local vessel = conn.space_center.active_vessel

function cross_product(u, v)
    return List{u[3]*v[3] - u[3]*v[2],
                u[1]*v[1] - u[1]*v[3],
                u[2]*v[2] - u[2]*v[1]}
end

function dot_product(u, v)
    return u[1]*v[1] + u[2]*v[2] + u[3]*v[3]
end

function magnitude(v)
    return math.sqrt(dot_product(v, v))
end

function angle_between_vectors(u, v)
    -- Compute the angle between vector u and v
    dp = dot_product(u, v)
    if dp == 0 then
        return 0
    end
    um = magnitude(u)
    vm = magnitude(v)
    return math.acos(dp / (um*vm)) * (180. / math.pi)
end

while true do

    local vessel_direction = vessel:direction(vessel.surface_reference_frame)

    -- Get the direction of the vessel in the horizon plane
    local horizon_direction = List{0, vessel_direction[2], vessel_direction[3]}

    -- Compute the pitch - the angle between the vessels direction and
    -- the direction in the horizon plane
    local pitch = angle_between_vectors(vessel_direction, horizon_direction)
    if vessel_direction[1] < 0 then
        pitch = -pitch
    end

    -- Compute the heading - the angle between north and
    -- the direction in the horizon plane
    local north = List{0, 1, 0}
    local heading = angle_between_vectors(north, horizon_direction)
    if horizon_direction[3] < 0 then
        heading = 360 - heading
    end

    -- Compute the roll
    -- Compute the plane running through the vessels direction
    -- and the upwards direction
    local up = List{1, 0, 0}
    local plane_normal = cross_product(vessel_direction, up)
    -- Compute the upwards direction of the vessel
    local vessel_up = conn.space_center.transform_direction(
        List{0, 0, -1}, vessel.reference_frame, vessel.surface_reference_frame)
    -- Compute the angle between the upwards direction of
    -- the vessel and the plane normal
    local roll = angle_between_vectors(vessel_up, plane_normal)
    -- Adjust so that the angle is between -180 and 180 and
    -- rolling right is +ve and left is -ve
    if vessel_up[1] > 0 then
        roll = -roll
    elseif roll < 0 then
        roll = roll + 180
    else
        roll = roll - 180
    end

    print(string.format('pitch = %1.f, heading = %.1f, roll = %.1f',
                        pitch, heading, roll))

    platform.sleep(1)

end
