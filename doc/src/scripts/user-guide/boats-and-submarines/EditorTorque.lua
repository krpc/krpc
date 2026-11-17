local krpc = require 'krpc'
local conn = krpc.connect()
local space_center = conn.space_center
local body = space_center.bodies['Kerbin']
-- [docs]
local vessel = space_center.editor.vessel
local flight = vessel:flight(vessel.reference_frame)
-- The vessel frame's z axis points out of the bottom, so up is its -z axis
local side = math.sqrt(0.5)
flight.surface_reference_frame =
  space_center.ReferenceFrame.create_relative(
    vessel.reference_frame, {0, 0, 0}, {0, side, 0, side})
local torque =
  flight:buoyant_torque_at(body.ocean_density, body.surface_gravity)
-- [/docs]
print(string.format('Torque = (%.0f, %.0f, %.0f) N m',
                    torque[1], torque[2], torque[3]))
