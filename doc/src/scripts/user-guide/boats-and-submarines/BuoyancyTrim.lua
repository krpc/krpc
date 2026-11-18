local krpc = require 'krpc'

local conn = krpc.connect('Buoyancy Trim')
local vessel = conn.space_center.active_vessel
local body = vessel.orbit.body

if not body.has_ocean then
  error(body.name .. ' has no ocean')
end

local flight = vessel:flight(vessel.surface_reference_frame)

-- The fully submerged figures, which do not depend on where the vessel is
local buoyancy =
  flight:buoyant_force_at(body.ocean_density, body.surface_gravity)
local torque =
  flight:buoyant_torque_at(body.ocean_density, body.surface_gravity)
local weight = vessel.mass * body.surface_gravity

print(string.format('Displacement:  %.1f m^3', flight.displacement))
print(string.format('Buoyancy:      %.0f N fully submerged', buoyancy))
print(string.format('Weight:        %.0f N', weight))
print(string.format('Floats:        %s', tostring(buoyancy > weight)))
print(string.format('Torque:        %.0f N m fully submerged',
                    math.sqrt(torque[1]^2 + torque[2]^2 + torque[3]^2)))

-- The trim, read in the surface reference frame. Its x axis points up, so
-- the y and z axes span the horizontal. A horizontal offset between the two
-- centers tips the hull until they line up again.
local center_of_buoyancy = flight.center_of_buoyancy

if center_of_buoyancy == nil then
  print('Trim:          vessel is out of the water')
else
  local center_of_mass = flight.center_of_mass
  local north = center_of_buoyancy[2] - center_of_mass[2]
  local east = center_of_buoyancy[3] - center_of_mass[3]
  print(string.format('Submerged:     %.0f%%',
                      100 * flight.submerged_portion))
  print(string.format('Trim:          %.2f m horizontally',
                      math.sqrt(north^2 + east^2)))
  print(string.format('Trim north:    %+.2f m', north))
  print(string.format('Trim east:     %+.2f m', east))
end
