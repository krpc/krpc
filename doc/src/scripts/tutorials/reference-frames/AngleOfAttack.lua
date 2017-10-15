local krpc = require 'krpc'
local platform = require 'krpc.platform'
local math = require 'math'
local conn = krpc.connect('Angle of attack')
local vessel = conn.space_center.active_vessel

while true do

   d = vessel:direction(vessel.orbit.body.reference_frame)
    v = vessel:velocity(vessel.orbit.body.reference_frame)

    -- Compute the dot product of d and v
    dotprod = d[1]*v[1] + d[2]*v[2] + d[3]*v[3]

    -- Compute the magnitude of v
    vmag = math.sqrt(v[1]*v[1] + v[2]*v[2] + v[3]*v[3])
    -- Note: don't need to magnitude of d as it is a unit vector

    -- Compute the angle between the vectors
    angle = 0
    if dotprod > 0 then
        angle = math.abs(math.acos (dotprod / vmag) * (180. / math.pi))
    end

    print(string.format('Angle of attack = %.1f', angle))

    platform.sleep(1)

end
