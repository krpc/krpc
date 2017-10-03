local krpc = require 'krpc'
local conn = krpc.connect('Example')
local vessel = conn.space_center.active_vessel

for _,parachute in ipairs(vessel.parts.parachutes) do
    parachute:deploy()
end
