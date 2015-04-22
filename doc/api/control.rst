Control
=======

.. class:: Control

   Used to manipulate the controls of a vessel. This includes adjusting the
   throttle, enabling/disabling systems such as SAS and RCS, or altering the
   direction in which the vessel is pointing.

   .. attribute:: SAS

      Gets or sets the state of SAS.

      :rtype: bool
      :returns: ``true`` if SAS is enabled, ``false`` if it is not.

      .. note:: Equivalent to :attr:`AutoPilot.SAS`

   .. attribute:: RCS

      Gets or sets the state of RCS.

      :rtype: bool
      :returns: ``true`` if RCS is enabled, ``false`` if it is not.

   .. attribute:: Gear

      Gets or sets the state of the gear/landing legs.

      :rtype: bool
      :returns: ``true`` if gear/landing legs are lowered, ``false`` if not.

   .. attribute:: Lights

      Gets or sets the state of the lights.

      :rtype: bool
      :returns: ``true`` if lights are enabled, ``false`` if they are not.

   .. attribute:: Brakes

      Gets or sets the state of the wheel brakes.

      :rtype: bool
      :returns: ``true`` if wheel brakes are enabled, ``false`` if they are not.

   .. attribute:: Abort

      Gets or sets the state of the abort action group.

      :type: bool

   .. attribute:: Throttle

      Gets or sets to state of the throttle. A value between 0 and 1.

      :type: float

   .. attribute:: Pitch

      Gets or sets the state of the pitch control (equivalent to the *w* and *s*
      keys) [#control-reset]_. A value between -1 and 1.

      :type: float

   .. attribute:: Yaw

      Gets or sets the state of the yaw control (equivalent to the *a* and *d*
      keys) [#control-reset]_. A value between -1 and 1.

      :type: float

   .. attribute:: Roll

      Gets or sets the state of the roll control (equivalent to the *q* and *e*
      keys) [#control-reset]_. A value between -1 and 1.

      :type: float

   .. attribute:: Forward

      Gets or sets the state of the forward translational control (equivalent to
      the *h* and *n* keys) [#control-reset]_. A value between -1 and 1.

      :type: float

   .. attribute:: Up

      Gets or sets the state of the up translational control (equivalent to the
      *i* and *k* keys) [#control-reset]_. A value between -1 and 1.

      :type: float

   .. attribute:: Right

      Gets or sets the state of the sideways translational control (equivalent
      to the *j* and *l* keys) [#control-reset]_. A value between -1 and 1.

      :type: float

   .. attribute:: WheelThrottle

      Gets or sets the state of the wheel throttle [#control-reset]_. A value
      between -1 and 1. A value of 1 rotates the wheels forwards, a value of -1
      rotates the wheels backwards.

      :type: float

   .. attribute:: WheelSteering

      Gets or sets the state of the wheel steering [#control-reset]_. A value
      between -1 and 1. A value of 1 steers to the left, and a value of -1
      steers to the right.

      :type: float

   .. attribute:: CurrentStage

      Gets the current stage of the vessel. Corresponds to the stage number in
      the in-game UI.

      :rtype: int32

   .. method:: ActivateNextStage ()

      Activates the next stage. Equivalent to pressing the space bar in-game.

      :rtype: :class:`List` ( :class:`Vessel` )
      :return: A list of vessel objects that are jettisoned from the active vessel.

   .. method:: GetActionGroup (group)

      Returns ``true`` if the given action group (a value between 0 and 9
      inclusive) is enabled.

      :ptype group: uint32
      :rtype: bool

   .. method:: SetActionGroup (group, state)

      Sets the state of the given action group (a value between 0 and 9
      inclusive).

      :ptype group: uint32
      :ptype state: bool

   .. method:: ToggleActionGroup (group)

      Toggles the state of the given action group (a value between 0 and 9
      inclusive).

      :ptype group: uint32

   .. method:: AddNode (UT, [prograde = 0], [normal = 0], [radial = 0])

      Creates a maneuver node at the given universal time, and returns a
      :class:`Node` object that can be used to modify it. Optionally sets the
      magnitude of the delta-v for the maneuver node in the prograde, normal and
      radial directions.

      :param double ut: universal time of the maneuver node
      :param float prograde: delta-v in the prograde direction
      :param float normal: delta-v in the normal direction
      :param float radial: delta-v in the radial direction
      :rtype: :class:`Node`

   .. attribute:: Nodes

      Gets a list of all existing maneuver nodes, ordered by time from first to
      last.

      :rtype: :class:`List` ( :class:`Node` )

   .. method:: RemoveNodes ()

      Removes all maneuver nodes.

.. rubric:: Footnotes

.. [#control-reset] The control input will persist until the client that
                    requested it disconnects. If multiple clients set a control
                    input, they are added together and clamped to the range
                    [-1,1].
