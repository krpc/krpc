InfernalRobotics API
====================

Provides RPCs to interact with the `InfernalRobotics`_ mod. Provides the
following classes:

.. toctree::
   :includehidden:
   :maxdepth: 2

   infernal-robotics/infernal-robotics
   infernal-robotics/control-group
   infernal-robotics/servo

Example
-------

The following example gets the control group named "MyGroup", prints out the
names and positions of all of the servos in the group, then moves all of the
servos to the right for 1 second.

#if $language == 'python'

.. code-block:: python

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

#else if $language == 'lua'

.. code-block:: lua

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

#end if

.. _InfernalRobotics: http://forum.kerbalspaceprogram.com/threads/116064
