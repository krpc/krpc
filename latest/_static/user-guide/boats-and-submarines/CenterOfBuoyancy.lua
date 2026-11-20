local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
-- [docs]
local flight = vessel:flight(vessel.surface_reference_frame)
local center_of_buoyancy = flight.center_of_buoyancy
local center_of_mass = flight.center_of_mass
-- [/docs]
if center_of_buoyancy ~= nil then
  local north = center_of_buoyancy[2] - center_of_mass[2]
  local east = center_of_buoyancy[3] - center_of_mass[3]
  print(string.format('Trim north = %.2f m, east = %.2f m', north, east))
end
