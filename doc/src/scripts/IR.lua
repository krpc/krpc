local krpc = require 'krpc.init'
local platform = require 'krpc.platform'
local Types = require 'krpc.types'

local conn = krpc.connect(nil, nil, nil, 'InfernalRobotics Example')

local group = conn.infernal_robotics.servo_group_with_name('MyGroup')
if group == Types.none then
  print('Group not found')
  os.exit(1)
end

for _,servo in ipairs(group.servos) do
  print(servo.name, servo.position)
end

group:move_right()
platform.sleep(1)
group:stop()
