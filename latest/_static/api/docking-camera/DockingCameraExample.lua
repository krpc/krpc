local krpc = require 'krpc'

local conn = krpc.connect('Docking Camera Example')
local parts = conn.space_center.active_vessel.parts
local part = parts:with_name('OnboardCamera')[1]
local camera = conn.docking_camera.camera(part)
local file = io.open('camera.png', 'wb')
file:write(camera:image())
file:close()
