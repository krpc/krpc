local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel

vessel.name = 'My Vessel'

local flight_info = vessel:flight()
print(flight_info.mean_altitude)
