ControlGroup
============

.. class:: ControlGroup

   A group of servos, obtained by calling :attr:`InfernalRobotics.ServoGroups`
   or :attr:`InfernalRobotics.ServoGroupWithName`. Represents the "Servo Groups"
   in the InfernalRobotics UI.

   .. attribute:: Name

      Gets of sets the name of the group.

      :rtype: string

   .. attribute:: ForwardKey

      Gets or sets the key assigned to be the "forward" key for the group.

      :rtype: string

   .. attribute:: ReverseKey

      Gets or sets the key assigned to be the "reverse" key for the group.

      :rtype: string

   .. attribute:: Speed

      Gets or sets the speed multiplier for the group.

      :rtype: float

   .. attribute:: Expanded

      Gets or sets whether the group is expanded in the InfernalRobotics UI.

      :rtype: bool

   .. attribute:: Servos

      Gets the servos that are in the group.

      :rtype: :class:`List` ( :class:`Servo` )

   .. method:: ServoWithName (name)

      Returns the servo with the given *name* from this group, or ``null`` if
      none exists.

      :param string name: name of servo to find
      :rtype: :class:`Servo`

   .. method:: MoveRight ()

      Moves all of the servos in the group to the right.

   .. method:: MoveLeft ()

      Moves all of the servos in the group to the left.

   .. method:: MoveCenter ()

      Moves all of the servos in the group to the center.

   .. method:: MoveNextPreset ()

      Moves all of the servos in the group to the next preset.

   .. method:: MovePrevPreset ()

      Moves all of the servos in the group to the previous preset.

   .. method:: Stop ()

      Stops the servos in the group.
