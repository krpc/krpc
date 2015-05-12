InfernalRobotics
================

.. class:: InfernalRobotics

   This service provides functionality to interact with the `InfernalRobotics`_
   mod.

   .. attribute:: ServoGroups

      Gets a list of all the servo groups in the active vessel.

      :rtype: :class:`List` ( :class:`ControlGroup` )

   .. method:: ServoGroupWithName (name)

      Returns the servo group with the given *name* or ``null`` if none
      exists. If multiple servo groups have the same name, only one of them is
      returned.

      :param string name: Name of servo group to find
      :rtype: :class:`ControlGroup`

   .. method:: ServoWithName (name)

      Returns the servo with the given *name*, from all servo groups, or
      ``null`` if none exists. If multiple servos have the same name, only one
      of them is returned.

      :param string name: Name of servo to find
      :rtype: :class:`Servo`

.. _InfernalRobotics: http://forum.kerbalspaceprogram.com/threads/116064
