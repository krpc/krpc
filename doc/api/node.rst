Node
====

.. class:: Node

   Represents a maneuver node. Can be created using :meth:`Control.AddNode`.

   .. attribute:: Prograde

      Gets or sets the magnitude of the maneuver nodes delta-v in the prograde
      direction, in meters per second.

      :rtype: double

   .. attribute:: Normal

      Gets or sets the magnitude of the maneuver nodes delta-v in the normal
      direction, in meters per second.

      :rtype: double

   .. attribute:: Radial

      Gets or sets the magnitude of the maneuver nodes delta-v in the radial
      direction, in meters per second.

      :rtype: double

   .. attribute:: DeltaV

      Gets or sets the delta-v of the maneuver node, in meters per second.

      :rtype: double

      .. note:: Does not change when executing the maneuver node. See
                :meth:`Node.RemainingDeltaV`.

   .. attribute:: RemainingDeltaV

      Gets the remaining delta-v of the maneuver node, in meters per
      second. Changes as the node is executed. This is equivalent to the delta-v
      reported in-game.

      :rtype: double

   .. method:: BurnVector ([referenceFrame = Vessel.OrbitalReferenceFrame])

      Returns a vector whose direction the direction of the maneuver node burn,
      and whose magnitude is the delta-v of the burn in m/s.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

      .. note:: Does not change when executing the maneuver node. See
                :meth:`Node.RemainingBurnVector`.

   .. method:: RemainingBurnVector ([referenceFrame = Vessel.OrbitalReferenceFrame])

      Returns a vector whose direction the direction of the maneuver node burn,
      and whose magnitude is the delta-v of the burn in m/s. The direction and
      magnitude change as the burn is executed.

      :rtype: double

   .. attribute:: UT

      Gets or sets the universal time at which the maneuver will occur, in
      seconds.

      :rtype: double

   .. attribute:: TimeTo

      Gets the time until the maneuver node will be encountered, in seconds.

      :rtype: double

   .. attribute:: Orbit

      Gets the orbit that results from executing the maneuver node.

      :rtype: :class:`Orbit`

   .. method:: Remove ()

      Removes the maneuver node.

   .. attribute:: ReferenceFrame

      Gets the reference frame that is fixed relative to the maneuver node's burn.

      * The origin is at the position of the maneuver node.

      * The y-axis points in the direction of the burn.

      * The x-axis and z-axis point in arbitrary but fixed directions.

      :rtype: :class:`ReferenceFrame`

   .. attribute:: OrbitalReferenceFrame

      Gets the reference frame that is fixed relative to the maneuver node, and
      orientated with the orbital prograde/normal/radial directions of the
      original orbit at the maneuver node's position.

      * The origin is at the position of the maneuver node.

      * The x-axis points in the orbital anti-radial direction of the original
        orbit, at the position of the maneuver node.

      * The y-axis points in the orbital prograde direction of the original
        orbit, at the position of the maneuver node.

      * The z-axis points in the orbital normal direction of the original orbit,
        at the position of the maneuver node.

      :rtype: :class:`ReferenceFrame`

   .. method:: Position (referenceFrame)

      Returns the position vector of the maneuver node in the given reference
      frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

   .. method:: Direction (referenceFrame)

      Returns the unit direction vector of the maneuver nodes burn in the given
      reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`
