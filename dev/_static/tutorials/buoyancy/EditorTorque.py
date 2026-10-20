import math

import krpc

conn = krpc.connect()
space_center = conn.space_center
body = space_center.bodies["Kerbin"]
# [docs]
vessel = space_center.editor.vessel
flight = vessel.flight(vessel.reference_frame)
# The vessel frame's z axis points out of the bottom, so up is its -z axis
side = math.sqrt(0.5)
flight.surface_reference_frame = space_center.ReferenceFrame.create_relative(
    vessel.reference_frame, rotation=(0, side, 0, side)
)
torque = flight.buoyant_torque_at(body.ocean_density, body.surface_gravity)
# [/docs]
