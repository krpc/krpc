SpaceCenter
===========

.. class:: SpaceCenter

   The SpaceCenter service provides functionality to interact with Kerbal Space
   Program. This includes controlling the active vessel, managing it's
   resources, planning maneuver nodes and auto-piloting.

   .. attribute:: ActiveVessel

      Gets the currently active vessel.

      :rtype: :class:`Vessel`

   .. attribute:: Vessels

      Gets a list of all the vessels in the game.

      :rtype: :class:`List` ( :class:`Vessel` )

   .. attribute:: Bodies

      Gets a dictionary of all celestial bodies (planets, moons, etc.) in the game,
      keyed by the name of the body.

      :rtype: :class:`Dictionary` ( string, :class:`CelestialBody` )

   .. attribute:: TargetBody

      Gets or sets the currently targeted celestial body.

      :rtype: :class:`CelestialBody`

   .. attribute:: TargetVessel

      Gets or sets the currently targeted vessel.

      :rtype: :class:`Vessel`

   .. attribute:: TargetDockingPort

      Gets or sets the currently targeted docking port.

      :rtype: :class:`DockingPort`

   .. method:: ClearTarget ()

      Clears the current target.

   .. attribute:: UT

      Gets the current universal time in seconds.

      :rtype: double

   .. attribute:: G

      Gets the value of the `gravitational constant
      <http://en.wikipedia.org/wiki/Gravitational_constant>`_ G in
      :math:`N(m/kg)^2`.

      :rtype: float

   .. attribute:: WarpMode

      Gets the current time warp mode. Returns :attr:`WarpMode.None` if time
      warp is not active, :attr:`WarpMode.Rails` if regular "on-rails" time warp
      is active, or :attr:`WarpMode.Physics` if physical time warp is active.

      :rtype: :class:`WarpMode`

   .. attribute:: WarpRate

      Gets the current warp rate. This is the rate at which time is passing for
      either on-rails or physical time warp. For example, a value of 10 means
      time is passing 10x faster than normal. Returns 1 if time warp is not
      active.

      :rtype: float

   .. attribute:: WarpFactor

      Gets the current warp factor. This is the index of the rate at which time
      is passing for either regular "on-rails" or physical time warp. Returns 0
      if time warp is not active. When in on-rails time warp, this is equal to
      :attr:`RailsWarpFactor`, and in physics time warp, this is equal to
      :attr:`PhysicsWarpFactor`.

      :rtype: int

   .. attribute:: RailsWarpFactor

      Gets or sets the time warp rate, using regular "on-rails" time warp. A
      value between 0 and 7 inclusive. 0 means no time warp. Returns 0 if
      physical time warp is active.

      If requested time warp factor cannot be set, it will be set to the next
      lowest possible value. For example, if the vessel is too close to a
      planet. See `the KSP wiki
      <http://wiki.kerbalspaceprogram.com/wiki/Time_warp>`_ for details.

      :rtype: int

   .. attribute:: PhysicsWarpFactor

      Gets or sets the physical time warp rate. A value between 0 and 3
      inclusive. 0 means no time warp. Returns 0 if regular "on-rails" time warp
      is active.

      :rtype: int

   .. method:: CanRailsWarpAt (factor)

      Returns true if regular "on-rails" time warp can be used, at the specified
      warp *factor*. The maximum time warp rate is limited by various factors --
      including how close the active vessel is to a planet. See `the KSP wiki
      <http://wiki.kerbalspaceprogram.com/wiki/Time_warp>`_ for details.

      :param bool factor: The warp factor to check.
      :rtype: bool

   .. attribute:: MaximumRailsWarpFactor

      Gets the current maximum regular "on-rails" warp factor that can be set. A
      value between 0 and 7 inclusive. See `the KSP wiki
      <http://wiki.kerbalspaceprogram.com/wiki/Time_warp>`_ for details how the
      maximum warp factor is limited.

      :rtype: int

   .. method:: WarpTo (UT, [maxRailsRate = 100000], [maxPhysicsRate = 2])

      Uses time acceleration to warp forward to a time in the future, specified
      by universal time *UT*. This call blocks until the desired time is
      reached. Uses regular "on-rails" or physical time warp as appropriate. For
      example, physical time warp is used when the active vessel is traveling
      through an atmosphere. When using regular "on-rails" time warp, the warp
      rate is limited by *maxRailsRate*, and when using physical time warp, the
      warp rate is limited by *maxPhysicsRate*.

      :param double ut: The universal time to warp to, in seconds
      :param float maxRailsRate: The maximum warp rate in regular "on-rails"
                                 time warp
      :param float maxPhysicsRate: The maximum warp rate in physical time warp
      :returns: When the time warp is complete.

   .. method:: TransformPosition (position, from, to)

      Converts a position vector from one reference frame to another.

      :param Vector3 position: Position vector in reference frame *from*.
      :param ReferenceFrame from: The reference frame that the position vector is in.
      :param ReferenceFrame to: The reference frame to covert the position vector to.
      :return: The corresponding position vector in reference frame *to*.
      :rtype: :class:`Vector3`

   .. method:: TransformDirection (direction, from, to)

      Converts a direction vector from one reference frame to another.

      :param Vector3 direction: Direction vector in reference frame *from*.
      :param ReferenceFrame from: The reference frame that the direction vector is in.
      :param ReferenceFrame to: The reference frame to covert the direction vector to.
      :return: The corresponding direction vector in reference frame *to*.
      :rtype: :class:`Vector3`

   .. method:: TransformRotation (rotation, from, to)

      Converts a rotation from one reference frame to another.

      :param Quaternion direction: Rotation in reference frame *from*.
      :param ReferenceFrame from: The reference frame that the rotation is in.
      :param ReferenceFrame to: The reference frame to covert the rotation to.
      :return: The corresponding rotation in reference frame *to*.
      :rtype: :class:`Quaternion`

   .. method:: TransformVelocity (position, velocity, from, to)

      Converts a velocity vector (acting at the specified position vector) from one
      reference frame to another. The position vector is required to take the
      relative angular velocity of the reference frames into account.

      :param Vector3 position: Position vector in reference frame *from*.
      :param Vector3 velocity: Velocity vector in reference frame *from*.
      :param ReferenceFrame from: The reference frame that the position and
                                  velocity vectors are in.
      :param ReferenceFrame to: The reference frame to covert the velocity vector to.
      :return: The corresponding velocity in reference frame *to*.
      :rtype: :class:`Vector3`

   .. attribute:: FARAvailable

      Gets whether `Ferram Aerospace Research`_ is installed.

      :rtype: bool

   .. attribute:: RemoteTechAvailable

      Gets whether `RemoteTech`_ is installed.

      :rtype: bool

   .. method:: DrawDirection (direction, referenceFrame, color, [length = 10])

      Draw a direction vector on the active vessel.

      :param Vector3 direction: Direction to draw the line in.
      :param ReferenceFrame referenceFrame: Reference frame that the direction is in.
      :param Vector3 color: The color to use for the line, as an r,g,b color.
      :param float length: The length of the line. Defaults to 10.

   .. method:: DrawLine (start, end, referenceFrame, color)

      Draw a line.

      :param Vector3 start: Position of the start of the line.
      :param Vector3 end: Position of the end of the line.
      :param ReferenceFrame referenceFrame: Reference frame that the position are in.
      :param Vector3 color: The color to use for the line, as an r,g,b color.

   .. method:: ClearDrawing ()

      Remove all directions and lines currently being drawn.

.. class:: WarpMode

   Returned by :attr:`SpaceCenter.WarpMode`.

   .. data:: Rails

      Time warp is active, and in regular "on-rails" mode.

   .. data:: Physics

      Time warp is active, and in physical time warp mode.

   .. data:: None

      Time warp is not active.

.. _Ferram Aerospace Research: http://forum.kerbalspaceprogram.com/threads/20451-0-90-Ferram-Aerospace-Research-v0-14-6-12-27-14
.. _RemoteTech: http://forum.kerbalspaceprogram.com/threads/83305-0-90-0-RemoteTech-v1-6-3-2015-02-06
