local krpc = require 'krpc'
local platform = require 'krpc.platform'
local List = require 'pl.List'
local math = require 'math'

local conn = krpc.connect('Landing Site')
local vessel = conn.space_center.active_vessel
local body = vessel.orbit.body
local ReferenceFrame = conn.space_center.ReferenceFrame

-- Define the landing site as the top of the VAB
local landing_latitude = -(0+(5.0/60)+(48.38/60/60))
local landing_longitude = -(74+(37.0/60)+(12.2/60/60))
local landing_altitude = 111

-- Determine landing site reference frame
-- (orientation: x=zenith, y=north, z=east)
local landing_position = body:surface_position(
  landing_latitude, landing_longitude, body.reference_frame)
local q_long = List{
  0,
  math.sin(-landing_longitude * 0.5 * math.pi / 180),
  0,
  math.cos(-landing_longitude * 0.5 * math.pi / 180)
}
local q_lat = List{
  0,
  0,
  math.sin(landing_latitude * 0.5 * math.pi / 180),
  math.cos(landing_latitude * 0.5 * math.pi / 180)
}
local landing_reference_frame =
  ReferenceFrame.create_relative(
    ReferenceFrame.create_relative(
      ReferenceFrame.create_relative(
        body.reference_frame,
        landing_position,
        q_long),
      List{0, 0, 0},
      q_lat),
    List{landing_altitude, 0, 0})

-- Draw axes
conn.drawing.add_line(List{0, 0, 0}, List{1, 0, 0}, landing_reference_frame)
conn.drawing.add_line(List{0, 0, 0}, List{0, 1, 0}, landing_reference_frame)
conn.drawing.add_line(List{0, 0, 0}, List{0, 0, 1}, landing_reference_frame)

while true do
   platform.sleep(1)
end
