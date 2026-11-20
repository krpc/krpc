local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
local part = vessel.parts:with_title('Clamp-O-Tron Docking Port')[1]
vessel.parts.controlling = part
