local krpc = require 'krpc'
local conn = krpc.connect('Example')
local vessel = conn.space_center.active_vessel

for parachute in vessel.parts.parachutes do
    parachute:deploy()
end
