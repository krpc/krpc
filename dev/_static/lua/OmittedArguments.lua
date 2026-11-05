local krpc = require 'krpc'
local conn = krpc.connect()

-- Launch with the default crew, without recovering the vessel on the pad
conn.space_center.launch_vessel('VAB', 'My Rocket', 'LaunchPad', nil, false)
