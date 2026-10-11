local krpc = require 'krpc'
local platform = require 'krpc.platform'

local conn = krpc.connect('Landing Site')
local vessel = conn.space_center.active_vessel
local body = vessel.orbit.body

-- Define the landing site as the top of the VAB
local landing_latitude = -(0+(5.0/60)+(48.38/60/60))
local landing_longitude = -(74+(37.0/60)+(12.2/60/60))
local landing_altitude = 111

-- Determine landing site reference frame
-- (orientation: x=zenith, y=north, z=east)
local landing_reference_frame = body:surface_reference_frame(
  landing_latitude, landing_longitude, landing_altitude)

-- Draw axes
conn.drawing.add_axes(landing_reference_frame)

while true do
   platform.sleep(1)
end
