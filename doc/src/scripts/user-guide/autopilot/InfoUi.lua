local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
-- [docs]
vessel.auto_pilot.show_info_ui = true
-- [/docs]
