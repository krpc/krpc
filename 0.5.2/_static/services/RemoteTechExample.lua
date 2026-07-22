local krpc = require 'krpc'
local math = require 'math'
local conn = krpc.connect('RemoteTech Example')
local vessel = conn.space_center.active_vessel

-- Set a dish target
local part = vessel.parts:with_title('Reflectron KR-7')[1]
local antenna = conn.remote_tech:antenna(part)
antenna.target_body = conn.space_center.bodies['Jool']

-- Get info about the vessels communications
local comms = conn.remote_tech:comms(vessel)
print('Signal delay = ' .. comms.signal_delay)
