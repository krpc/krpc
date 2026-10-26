local krpc = require 'krpc'
local platform = require 'krpc.platform'
local conn = krpc.connect('Launch')
local vehicle = conn.ksa:launch_vehicle('Rocket', 'CCSFS LC-39A')
local flight = vehicle.flight

vehicle.auto_pilot:target_pitch_and_heading(90, 90)
vehicle.auto_pilot:engage()
vehicle.control.throttle = 1
vehicle.control:activate_next_sequence()
vehicle.control.engine_on = true
print(vehicle.name .. ' has launched')

while flight.mean_altitude < 10000 do
    platform.sleep(0.1)
end

vehicle.control.engine_on = false
vehicle.auto_pilot:disengage()
print(vehicle.name .. ' reached 10 km')
