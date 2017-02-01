local krpc = require 'krpc'

local conn = krpc.connect('InfernalRobotics Example')
local vessel = conn.space_center.active_vessel

local group = conn.infernal_robotics.servo_group_with_name(vessel, 'MyGroup')
if group == krpc.types.none then
  print('Group not found')
  os.exit(1)
end

for _,servo in ipairs(group.servos) do
  print(servo.name, servo.position)
end

group:move_right()
krpc.platform.sleep(1)
group:stop()
