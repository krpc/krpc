local krpc = require 'krpc'
local platform = require 'krpc.platform'
local conn = krpc.connect('Vessel speed')
local vessel = conn.space_center.active_vessel
local obt_frame = vessel.orbit.body.non_rotating_reference_frame
local srf_frame = vessel.orbit.body.reference_frame

while true do
    obt_speed = vessel:flight(obt_frame).speed
    srf_speed = vessel:flight(srf_frame).speed
    print(string.format(
      'Orbital speed = %.1f m/s, Surface speed = %.1f m/s',
      obt_speed, srf_speed))
    platform.sleep(1)
end
