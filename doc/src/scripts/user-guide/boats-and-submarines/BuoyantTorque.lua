local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
local body = vessel.orbit.body
local flight = vessel:flight(vessel.surface_reference_frame)
-- [docs]
local torque =
  flight:buoyant_torque_at(body.ocean_density, body.surface_gravity)
-- [/docs]
print(string.format('Torque = (%.0f, %.0f, %.0f) N m',
                    torque[1], torque[2], torque[3]))
