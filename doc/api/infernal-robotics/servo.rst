Servo
=====

.. class:: Servo

   Represents a servo. Obtained using :attr:`ControlGroup.Servos` or
   :attr:`ControlGroup.ServoWithName` or :attr:`InfernalRobotics.ServoWithName`.

   .. attribute:: Name

      Gets or sets the name of the servo.

      :rtype: string

   .. attribute:: Highlight

      Sets whether the servo should be highlighted in-game.

      :rtype: bool

   .. attribute:: Position

      Gets the position of the servo.

      :rtype: float

   .. attribute:: MinConfigPosition

      Gets the minimum position of the servo, specified by the part configuration.

      :rtype: float

   .. attribute:: MaxConfigPosition

      Gets the maximum position of the servo, specified by the part configuration.

      :rtype: float

   .. attribute:: MinPosition

      Gets or sets the minimum position of the servo, specified by the in-game tweak menu.

      :rtype: float

   .. attribute:: MaxPosition

      Gets or sets the maximum position of the servo, specified by the in-game tweak menu.

      :rtype: float

   .. attribute:: ConfigSpeed

      Gets the speed multiplier of the servo, specified by the part configuration.

      :rtype: float

   .. attribute:: Speed

      Gets or sets the speed multiplier of the servo, specified by the in-game tweak menu.

      :rtype: float

   .. attribute:: CurrentSpeed

      Gets or sets the current speed at which the servo is moving.

      :rtype: float

   .. attribute:: Acceleration

      Gets or sets the current speed multiplier set in the UI.

      :rtype: float

   .. attribute:: IsMoving

      Gets whether the servo is moving.

      :rtype: bool

   .. attribute:: IsFreeMoving

      Gets whether the servo is freely moving.

      :rtype: bool

   .. attribute:: IsLocked

      Gets or sets whether the servo is locked.

      :rtype: bool

   .. attribute:: IsAxisInverted

      Gets or sets whether the servos axis is inverted.

      :rtype: bool

   .. method:: MoveRight ()

      Moves the servo to the right.

   .. method:: MoveLeft ()

      Moves the servo to the left.

   .. method:: MoveCenter ()

      Moves the servo to the center.

   .. method:: MoveNextPreset ()

      Moves the servo to the next preset.

   .. method:: MovePrevPreset ()

      Moves the servo to the previous preset.

   .. method:: MoveTo (position, speed)

      Moves the servo to *position* and sets the speed multiplier to *speed*.

      :param float position: the position to move the servo to
      :param float speed: speed multiplier for the movement

   .. method:: Stop ()

      Stops servo.
