local krpc = require 'krpc'

local conn = krpc.connect('Docking Camera Example')
local part = conn.space_center.active_vessel.parts:with_name('OnboardCamera')[1]
local camera = conn.docking_camera.camera(part)
local file = io.open('camera.png', 'wb')
file:write(camera:image())
file:close()
