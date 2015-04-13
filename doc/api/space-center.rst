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

      :rtype: double

   .. method:: WarpTo (UT, [maxRate = 100000])

      Uses time acceleration to warp to the specified time. Automatically uses
      regular or physical time warp as appropriate. For example, physical time warp
      is used when the active vessel is travelling through an atmosphere. When
      using physical time warp, the warp rate is at most 2x.

      :param double ut: The universal time to warp to, in seconds
      :param double maxRate: The maximum warp rate to use
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

   .. method:: ClearDirections ()

      Remove all directions currently being drawn.

.. _Ferram Aerospace Research: http://forum.kerbalspaceprogram.com/threads/20451-0-90-Ferram-Aerospace-Research-v0-14-6-12-27-14
.. _RemoteTech: http://forum.kerbalspaceprogram.com/threads/83305-0-90-0-RemoteTech-v1-6-3-2015-02-06
