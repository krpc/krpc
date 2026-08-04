local krpc = require 'krpc'
local platform = require 'krpc.platform'
local conn = krpc.connect('Navball speed')
local vessel = conn.space_center.active_vessel
local obt_frame = vessel.orbit_speed_reference_frame
local srf_frame = vessel.surface_speed_reference_frame

while true do
    obt_speed = vessel:flight(obt_frame).speed
    srf_speed = vessel:flight(srf_frame).speed
    print(string.format(
      'Orbital speed = %.1f m/s, Surface speed = %.1f m/s',
      obt_speed, srf_speed))
    platform.sleep(1)
end
