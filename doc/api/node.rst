Node
====

.. class:: Node

   Represents a maneuver node. Can be created using :meth:`Control.AddNode`.

.. attribute:: Node.Prograde

   Gets or sets the magnitude of the maneuver nodes delta-v in the prograde
   direction, in meters per second.

   :rtype: `double`

.. attribute:: Node.Normal

   Gets or sets the magnitude of the maneuver nodes delta-v in the normal direction,
   in meters per second.

   :rtype: `double`

.. attribute:: Node.Radial

   Gets or sets the magnitude of the maneuver nodes delta-v in the radial direction,
   in meters per second.

   :rtype: `double`

.. attribute:: Node.DeltaV

   Gets or sets the delta-v of the maneuver node, in meters per second.

   :rtype: `double`

   .. note:: Does not change when executing the maneuver node. See
             :meth:`Node.RemainingDeltaV`.

.. attribute:: Node.RemainingDeltaV

   Gets the remaining delta-v of the maneuver node, in meters per
   second. Changes as the node is executed. This is equivalent to the delta-v
   reported in-game.

   :rtype: `double`

.. method:: Node.BurnVector (referenceFrame)

   Returns a vector in the given reference frame whose:
     - direction is the direction of the maneuver nodes burn
     - magnitude is the delta-v of the maneuver node

   :param ReferenceFrame referenceFrame:
   :rtype: :class:`Vector3`

.. attribute:: Node.UT

   Gets or sets the universal time at which the maneuver will occur, in seconds.

   :rtype: `double`

.. attribute:: Node.TimeTo

   Gets the time until the maneuver node will be encountered, in seconds.

   :rtype: `double`

.. attribute:: Node.Orbit

   Gets the orbit that results from executing the maneuver node.

   :rtype: :class:`Orbit`

.. method:: Node.Remove ()

   Removes the maneuver node.

.. attribute:: Node.ReferenceFrame

   Gets the reference frame that is fixed relative to the maneuver node, and
   orientated with the direction of the burn.

   * The origin is at the position of the maneuver node.

   * The y-axis points in the direction of the burn.

   .. todo:: In which directions do the x-axis and z-axis point?

   :rtype: :class:`ReferenceFrame`

.. attribute:: Node.OrbitalReferenceFrame

   Gets the reference frame that is fixed relative to the maneuver node, and
   orientated with the orbital prograde/normal/radial directions of the original
   orbit at the position of the maneuver node.

   * The origin is at the position of the maneuver node.

   * The x-axis points in the orbital radial direction of the original orbit, at
     the position of the maneuver node.

   * The y-axis points in the orbital prograde direction of the original orbit,
     at the position of the maneuver node.

   :rtype: :class:`ReferenceFrame`

.. method:: Node.Position (referenceFrame)

   Returns the position vector of the maneuver node in the given reference
   frame.

   :param ReferenceFrame referenceFrame:
   :rtype: :class:`Vector3`

.. method:: Node.Direction (referenceFrame)

   Returns the unit direction vector of the maneuver nodes burn in the given
   reference frame.

   :param ReferenceFrame referenceFrame:
   :rtype: :class:`Vector3`
