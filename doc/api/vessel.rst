Vessel
======

.. class:: Vessel

   These objects are used to interact with vessels in KSP. This includes getting
   orbital and flight data, manipulating control inputs and managing resources.

.. attribute:: Vessel.Name

   Gets or sets the name of the vessel.

   :type: `string`

.. attribute:: Vessel.Type

   Gets or sets the type of the vessel.

   :type: :class:`VesselType`

.. attribute:: Vessel.Situation

   Gets the situation the vessel is in.

   :type: :class:`VesselSituation`

.. attribute:: Vessel.MET

   Gets the mission elapsed time in seconds.

   :rtype: `double`

.. method:: Vessel.Flight (referenceFrame = Vessel.Orbital)

   Gets a :class:`Flight` object that can be used to get flight telemetry for
   the vessel, in the specified reference frame.

   :param ReferenceFrame referenceFrame: Defaults to the orbital reference frame
                                         of the vessel.
   :rtype: :class:`Flight`

.. attribute:: Vessel.Target

   Gets or sets the target vessel. Returns `null` if there is no target. When
   setting the target, the target cannot be the current vessel.

   :type: :class:`Vessel`

.. attribute:: Vessel.Orbit

   Gets the current orbit of the vessel.

   :rtype: :class:`Orbit`

.. attribute:: Vessel.Control

   Gets a :class:`Control` object that can be used to manipulate the vessel's
   control inputs. For example, its pitch/yaw/roll controls, RCS and thrust.

   :rtype: :class:`Control`

.. attribute:: Vessel.AutoPilot

   Gets an :class:`AutoPilot` object, that can be used to perform simple
   auto-piloting of the vessel.

   :rtype: :class:`AutoPilot`

.. attribute:: Vessel.Resources

   Gets a :class:`Resources` object, that can used to get information about, and
   manage, the vessels resources.

   :rtype: :class:`Resources`

.. attribute:: Vessel.Comms

   Gets a :class:`Comms` object, that can used to interact with `RemoteTech`_
   for this vessel.

   :rtype: :class:`Comms`

   .. note:: Requires `RemoteTech`_ to be installed.

.. attribute:: Vessel.Mass

   Gets the total mass of the vessel (including resources) in kg.

   :rtype: `double`

.. attribute:: Vessel.DryMass

   Gets the total mass of the vessel (excluding resources) in kg.

   :rtype: `double`

.. attribute:: Vessel.CrossSectionalArea

   Gets the cross sectional area of the vessel in :math:`m^3`. See
   :attr:`Flight.Drag`.

   :rtype: `double`

   .. note:: Calculated using `Ferram Aerospace Research`_ if it is
      installed. Otherwise, calculated using `KSPs stock aerodynamic model`_

.. attribute:: Vessel.Thrust

   Gets the total thrust of all active engines combined in Newtons.

   :rtype: `double`

   .. note::
      Assumes all active engines are pointing in the same direction.

.. attribute:: Vessel.SpecificImpulse

   Gets the combined specific impulse of all active engines in seconds.

   :rtype: `double`

.. attribute:: Vessel.ReferenceFrame

   Gets the reference frame that is fixed relative to this vessel.
   The origin is at the center of mass of the vessel.
   The y-axis points in the direction the vessels controlling part is pointing.
   The x-axis and z-axis point in perpendicular directions out to the side of the vessel.

   :rtype: :class:`ReferenceFrame`

.. attribute:: Vessel.NonRotatingReferenceFrame

   Gets the reference frame whose origin is at the center of mass of the vessel,
   and whose axes point in an arbitrary but fixed direction.

   :rtype: :class:`ReferenceFrame`

.. attribute:: Vessel.OrbitalReferenceFrame

   Gets the reference frame relative to the orbit of this vessel.
   The origin is at the center of mass of the vessel.
   The x-axis points normal to the body being orbited (from the center of the
   body towards the center of mass of the vessel).
   The y-axis points to the north pole of the body being orbited.

   :rtype: :class:`ReferenceFrame`

.. attribute:: Vessel.SurfaceReferenceFrame

   Gets the reference frame relative to the surface of the body being orbited by
   this vessel.
   The origin is at the center of mass of the vessel.
   The x-axis points normal to the body being orbited (from the center of the
   body towards the center of mass of the vessel).
   The y-axis points to the north pole of the body being orbited.

   :rtype: :class:`ReferenceFrame`

.. method:: Vessel.Position (referenceFrame)

   Returns the position vector of the center of mass of the vessel in the given
   reference frame.

   :param ReferenceFrame referenceFrame:
   :rtype: :class:`Vector3`

.. method:: Vessel.Velocity (referenceFrame)

   Returns the velocity vector of the center of mass of the vessel in the given
   reference frame.

   :param ReferenceFrame referenceFrame:
   :rtype: :class:`Vector3`

.. method:: Vessel.Rotation (referenceFrame)

   Returns the rotation of the center of mass of the vessel in the given
   reference frame.

   :param ReferenceFrame referenceFrame:
   :rtype: :class:`Quaternion`

.. method:: Vessel.Direction (referenceFrame)

   Returns the direction in which the vessel is pointing, as a unit vector, in
   the given reference frame.

   :param ReferenceFrame referenceFrame:
   :rtype: :class:`Vector3`

.. method:: Vessel.AngularVelocity (referenceFrame)

   Returns the angular velocity of the vessel in the given reference frame. The
   magnitude of the returned vector is the rotational speed in radians per
   second, and the direction of the vector indicates the axis of rotation (using
   the right hand rule).

   :param ReferenceFrame referenceFrame:
   :rtype: :class:`Vector3`

.. _Ferram Aerospace Research: http://forum.kerbalspaceprogram.com/threads/20451-0-90-Ferram-Aerospace-Research-v0-14-6-12-27-14
.. _RemoteTech: http://forum.kerbalspaceprogram.com/threads/83305-0-90-0-RemoteTech-v1-6-3-2015-02-06
.. _KSPs stock aerodynamic model: http://wiki.kerbalspaceprogram.com/wiki/Atmosphere
