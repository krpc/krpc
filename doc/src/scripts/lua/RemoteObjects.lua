local krpc = require 'krpc'
local socket = require 'socket'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel
local flight_info = vessel:flight()

-- Each read gets the current altitude from the game
print(flight_info.mean_altitude)
socket.sleep(1)
print(flight_info.mean_altitude)

-- Two objects for the same vessel are equal
print(vessel == conn.space_center.active_vessel)
