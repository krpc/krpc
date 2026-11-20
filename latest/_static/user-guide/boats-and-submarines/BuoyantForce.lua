local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
-- [docs]
local body = vessel.orbit.body
local flight = vessel:flight()
local displacement = flight.displacement
local buoyancy =
  flight:buoyant_force_at(body.ocean_density, body.surface_gravity)
-- [/docs]
print(string.format('Displacement = %.1f m^3, buoyancy = %.0f N',
                    displacement, buoyancy))
