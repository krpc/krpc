local krpc = require 'krpc'
local conn = krpc.connect()
-- [docs]
local vessel = conn.space_center.active_vessel
local parachute = vessel.parts:with_name('parachuteSingle')[1].parachute
conn.space_center.quickload()
-- reads the parachute of the loaded game
print(parachute.state.value)
-- [/docs]
