local krpc = require 'krpc'
local platform = require 'krpc.platform'
local conn = krpc.connect('Orbital speed')
local vessel = conn.space_center.active_vessel
local ref_frame = conn.SpaceCenter.ReferenceFrame.CreateHybrid(
  vessel.orbit.body.reference_frame,
  vessel.surface_reference_frame)

while true do
    velocity = vessel:flight(ref_frame).velocity
    print(string.format('Surface velocity = (%.1f, %.1f, %.1f)',
                        velocity[1], velocity[2], velocity[3]))
    platform.sleep(1)
end
