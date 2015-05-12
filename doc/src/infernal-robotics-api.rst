InfernalRobotics API
====================

Provides RPCs to interact with the `InfernalRobotics`_ mod. Provides the
following classes:

.. toctree::
   :includehidden:
   :maxdepth: 2

   python-api/infernal-robotics/infernal-robotics
   python-api/infernal-robotics/control-group
   python-api/infernal-robotics/servo

Example
-------

The following example gets the control group named "MyGroup", prints out the
names and positions of all of the servos in the group, then moves all of the
servos to the right for 1 second.

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

.. _InfernalRobotics: http://forum.kerbalspaceprogram.com/threads/116064
