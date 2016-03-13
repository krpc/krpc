import krpc, time
conn = krpc.connect(name='InfernalRobotics Example')

group = conn.infernal_robotics.servo_group_with_name('MyGroup')
if group is None:
    print('Group not found')
    exit(1)

for servo in group.servos:
    print servo.name, servo.position

group.move_right()
time.sleep(1)
group.stop()
