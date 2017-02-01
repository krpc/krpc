local krpc = require 'krpc'
local platform = require 'krpc.platform'
local conn = krpc.connect('Surface speed')
local vessel = conn.space_center.active_vessel
local ref_frame = vessel.orbit.body.reference_frame

while true do
    velocity = vessel:flight(ref_frame).velocity
    print(string.format('Surface velocity = (%.1f, %.1f, %.1f)',
                        velocity[1], velocity[2], velocity[3]))

    speed = vessel:flight(ref_frame).speed
    print(string.format('Surface speed = %.1f m/s', speed))

    platform.sleep(1)
end
