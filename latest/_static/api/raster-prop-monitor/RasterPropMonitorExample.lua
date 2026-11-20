local krpc = require 'krpc'

local conn = krpc.connect('Raster Prop Monitor Example')
local vessel = conn.space_center.active_vessel
local rpm = conn.raster_prop_monitor

-- The first screen in the IVA, and the content of its krpc page
local monitor = rpm.monitors(vessel)[1]
local display = monitor.display

-- Raster Prop Monitor formats the SAS variable, and keeps it up to date
display.text = 'SAS: {0:ON;OFF;OFF} $&$ SAS'

-- A press of the NAV button shows the page
monitor:capture_button('buttonR1', 'krpc')
