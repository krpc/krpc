Control
=======

.. class:: Control

   Used to manipulate the controls of a vessel. This includes adjusting the
   throttle, enabling/disabling systems such as SAS and RCS, or altering the
   direction in which the vessel is pointing.

.. attribute:: Control.RCS

   Gets or sets the state of RCS.

   :rtype: `bool`
   :returns: `True` if RCS is enabled, `False` if it is not.

.. attribute:: Control.Gear

   Gets or sets the state of the gear/landing legs.

   :rtype: `bool`
   :returns: `True` if gear/landing legs are lowered, `False` if not.

.. attribute:: Control.Lights

   Gets or sets the state of the lights.

   :rtype: `bool`
   :returns: `True` if lights are enabled, `False` if they are not.

.. attribute:: Control.Brakes

   Gets or sets the state of the wheel brakes.

   :rtype: `bool`
   :returns: `True` if wheel brakes are enabled, `False` if they are not.

.. attribute:: Control.Abort

   Gets or sets the state of the abort action group.

   :type: `bool`

.. attribute:: Control.Throttle

   Gets or sets to state of the throttle. A value between 0 and 1.

   :type: `float`

.. attribute:: Control.Forward

   Gets or sets the state of the forward translational control (equivalent to
   the `h` and `n` keys). A value between -1 and 1.

   :type: `float`

.. attribute:: Control.Up

   Gets or sets the state of the up translational control (equivalent to the `i`
   and `k` keys). A value between -1 and 1.

   :type: `float`

.. attribute:: Control.Sideways

   Gets or sets the state of the sideways translational control (equivalent to
   the `j` and `l` keys). A value between -1 and 1.

   :type: `float`

.. attribute:: Control.Pitch

   Gets or sets the state of the pitch control (equivalent to the `w` and `s`
   keys). A value between -1 and 1.

   :type: `float`

.. attribute:: Control.Roll

   Gets or sets the state of the roll control (equivalent to the `q` and `e`
   keys). A value between -1 and 1.

   :type: `float`

.. attribute:: Control.Yaw

   Gets or sets the state of the yaw control (equivalent to the `a` and `d`
   keys). A value between -1 and 1.

   :type: `float`

.. attribute:: Control.WheelThrottle

   Gets or sets to state of the wheel throttle. A value between -1 and 1. A
   value of 1 rotates the wheels fowards, a value of -1 rotates the wheels
   backwards.

   :type: `float`

.. attribute:: Control.WheelSteering

   Gets or sets to state of the wheel steering. A value between -1 and 1. A
   value of 1 steers to the left, and a value of -1 steers to the right (using
   the right handed rule).

   :type: `float`

.. attribute:: Control.CurrentStage

   Gets the current stage of the vessel. Corresponds to the stage number in the
   in-game UI.

   :rtype: `int16`

.. method:: Control.ActivateNextStage ()

   Activates the next stage. Equivalent to pressing the space bar in-game.

   :rtype: :class:`List` ( :class:`Vessel` )
   :return: A list of vessel objects that are jettisoned from the active vessel.

.. method:: Control.GetActionGroup (group)

   Returns `True` if the given action group (a value between 0 and 9 inclusive)
   is enabled.

   :ptype group: `uint16`
   :rtype: `bool`

.. method:: Control.SetActionGroup (group, state)

   Sets the state of the given action group (a value between 0 and 9 inclusive).

   :ptype group: `uint16`
   :ptype state: `bool`

.. method:: Control.ToggleActionGroup (group)

   Toggles the state of the given action group (a value between 0 and 9
   inclusive).

   :ptype group: `uint16`

.. method:: Control.AddNode (ut, prograde = 0, normal = 0, radial = 0)

   Creates a maneuver node at the given universal time, and returns a
   :class:`Node` object that can be used to modify it. Optionally sets
   the magnitude of the delta-v for the maneuver node in the prograde, normal
   and radial directions.

   :param double ut: universal time of the maneuver node
   :param double prograde: delta-v in the prograde direction
   :param double normal: delta-v in the normal direction
   :param double radial: delta-v in the radial direction
   :rtype: :class:`Node`

.. attribute:: Control.Nodes

   Gets a list of all existing maneuver nodes, ordered by time from first to
   last.

   :rtype: :class:`List` ( :class:`Node` )

.. method:: Control.RemoveNodes ()

   Removes all maneuver nodes.
