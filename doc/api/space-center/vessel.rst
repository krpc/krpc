Vessel
======

.. class:: Vessel

   These objects are used to interact with vessels in KSP. This includes getting
   orbital and flight data, manipulating control inputs and managing resources.

   .. attribute:: Name

      Gets or sets the name of the vessel.

      :type: string

   .. attribute:: Type

      Gets or sets the type of the vessel.

      :type: :class:`VesselType`

   .. attribute:: Situation

      Gets the situation the vessel is in.

      :type: :class:`VesselSituation`

   .. attribute:: MET

      Gets the mission elapsed time in seconds.

      :rtype: double

   .. method:: Flight ([referenceFrame = Vessel.SurfaceReferenceFrame])

      Gets a :class:`Flight` object that can be used to get flight telemetry for
      the vessel, in the specified reference frame.

      :param ReferenceFrame referenceFrame: Defaults to the surface reference frame
                                            of the vessel.
      :rtype: :class:`Flight`

   .. attribute:: Target

      Gets or sets the target vessel. Returns ``null`` if there is no target. When
      setting the target, the target cannot be the current vessel.

      :type: :class:`Vessel`

   .. attribute:: Orbit

      Gets the current orbit of the vessel.

      :rtype: :class:`Orbit`

   .. attribute:: Control

      Gets a :class:`Control` object that can be used to manipulate the vessel's
      control inputs. For example, its pitch/yaw/roll controls, RCS and thrust.

      :rtype: :class:`Control`

   .. attribute:: AutoPilot

      Gets an :class:`AutoPilot` object, that can be used to perform simple
      auto-piloting of the vessel.

      :rtype: :class:`AutoPilot`

   .. attribute:: Resources

      Gets a :class:`Resources` object, that can used to get information about
      resources stored in the vessel.

   .. method:: ResourcesInDecoupleStage (stage, [cumulative = true])

      Gets a :class:`Resources` object, that can used to get information about
      resources stored in a given *stage*.

      :param int32 stage: Get resources for parts that are decoupled in this
                          stage. For details on stage numbering, see the
                          discussion on :ref:`api-parts-staging`.
      :param bool cumulative: When ``false``, returns the resources for parts
                              decoupled in just the given stage. When ``true``
                              returns the resources decoupled in the given stage
                              and all subsequent stages combined.
      :rtype: :class:`Resources`

   .. attribute:: Parts

      Gets a :class:`Parts` object, that can used to interact with the parts
      that make up this vessel.

      :rtype: :class:`Parts`

   .. attribute:: Comms

      Gets a :class:`Comms` object, that can used to interact with `RemoteTech`_
      for this vessel.

      :rtype: :class:`Comms`

      .. note:: Requires `RemoteTech`_ to be installed.

   .. attribute:: Mass

      Gets the total mass of the vessel (including resources) in kg.

      :rtype: float

   .. attribute:: DryMass

      Gets the total mass of the vessel (excluding resources) in kg.

      :rtype: float

   .. attribute:: Thrust

      Gets the total thrust currently being produced by the vessel's engines, in
      Newtons. This is computed by summing :attr:`Engine.Thrust` for every
      engine in the vessel.

      :rtype: float

   .. attribute:: AvailableThrust

      Gets the total available thrust that can be produced by the vessel's
      active engines, in Newtons. This is computed by summing
      :attr:`Engine.AvailableThrust` for every active engine in the vessel.

      :rtype: float

   .. attribute:: MaxThrust

      Gets the total maximum thrust that can be produced by the vessel's active
      engines, in Newtons. This is computed by summing :attr:`Engine.MaxThrust`
      for every active engine.

      :rtype: float

   .. attribute:: MaxVacuumThrust

      Gets the total maximum thrust that can be produced by the vessel's active
      engines when the vessel is in a vacuum, in Newtons. This is computed by
      summing :attr:`Engine.MaxVacuumThrust` for every active engine.

      :rtype: float

   .. attribute:: SpecificImpulse

      Gets the combined specific impulse of all active engines, in seconds. This
      is computed using the formula `described here
      <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines>`_.

      :rtype: float

   .. attribute:: VacuumSpecificImpulse

      Gets the combined vacuum specific impulse of all active engines, in
      seconds. This is computed using the formula `described here
      <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines>`_.

      :rtype: float

   .. attribute:: KerbinSeaLevelSpecificImpulse

      Gets the combined specific impulse of all active engines at sea level on
      Kerbin, in seconds. This is computed using the formula `described here
      <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines>`_.

      :rtype: float

   .. attribute:: ReferenceFrame

      Gets the reference frame that is fixed relative to the vessel, and orientated
      with the vessel.

      * The origin is at the center of mass of the vessel.

      * The axes rotate with the vessel.

      * The x-axis points out to the right of the vessel.

      * The y-axis points in the forward direction of the vessel.

      * The z-axis points out of the bottom off the vessel.

      :rtype: :class:`ReferenceFrame`

      .. figure:: /images/reference-frames/vessel-aircraft.png
         :align: center

         Vessel reference frame origin and axes for the Aeris 3A aircraft

      .. figure:: /images/reference-frames/vessel-rocket.png
         :align: center

         Vessel reference frame origin and axes for the Kerbal-X rocket

   .. attribute:: OrbitalReferenceFrame

      Gets the reference frame that is fixed relative to the vessel, and orientated
      with the vessels orbital prograde/normal/radial directions.

      * The origin is at the center of mass of the vessel.

      * The axes rotate with the orbital prograde/normal/radial directions.

      * The x-axis points in the orbital anti-radial direction.

      * The y-axis points in the orbital prograde direction.

      * The z-axis points in the orbital normal direction.

      :rtype: :class:`ReferenceFrame`

      .. note:: Be careful not to confuse this with 'orbit' mode on the navball.

      .. figure:: /images/reference-frames/vessel-orbital.png
         :align: center

         Vessel orbital reference frame origin and axes

   .. attribute:: SurfaceReferenceFrame

      Gets the reference frame that is fixed relative to the vessel, and orientated
      with the surface of the body being orbited.

      * The origin is at the center of mass of the vessel.

      * The axes rotate with the north and up directions on the surface of the
        body.

      * The x-axis points in the `zenith <http://en.wikipedia.org/wiki/Zenith>`_
        direction (upwards, normal to the body being orbited, from the center of
        the body towards the center of mass of the vessel).

      * The y-axis points northwards towards the `astronomical horizon
        <http://en.wikipedia.org/wiki/Horizon>`_ (north, and tangential to the
        surface of the body -- the direction in which a compass would point when
        on the surface).

      * The z-axis points eastwards towards the `astronomical horizon
        <http://en.wikipedia.org/wiki/Horizon>`_ (east, and tangential to the
        surface of the body -- east on a compass when on the surface).

      :rtype: :class:`ReferenceFrame`

      .. note:: Be careful not to confuse this with 'surface' mode on the navball.

      .. figure:: /images/reference-frames/vessel-surface.png
         :align: center

         Vessel surface reference frame origin and axes

   .. attribute:: SurfaceVelocityReferenceFrame

      Gets the reference frame that is fixed relative to the vessel, and orientated
      with the velocity vector of the vessel relative to the surface of the body
      being orbited.

      * The origin is at the center of mass of the vessel.

      * The axes rotate with the vessel's velocity vector.

      * The y-axis points in the direction of the vessel's velocity vector,
        relative to the surface of the body being orbited.

      * The z-axis is in the plane of the `astronomical horizon
        <http://en.wikipedia.org/wiki/Horizon>`_.

      * The x-axis is orthogonal to the other two axes.

      :rtype: :class:`ReferenceFrame`

      .. figure:: /images/reference-frames/vessel-surface-velocity.png
         :align: center

         Vessel surface velocity reference frame origin and axes

   .. method:: Position (referenceFrame)

      Returns the position vector of the center of mass of the vessel in the given
      reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

   .. method:: Velocity (referenceFrame)

      Returns the velocity vector of the center of mass of the vessel in the given
      reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

   .. method:: Rotation (referenceFrame)

      Returns the rotation of the center of mass of the vessel in the given
      reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Quaternion`

   .. method:: Direction (referenceFrame)

      Returns the direction in which the vessel is pointing, as a unit vector, in
      the given reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

   .. method:: AngularVelocity (referenceFrame)

      Returns the angular velocity of the vessel in the given reference frame. The
      magnitude of the returned vector is the rotational speed in radians per
      second, and the direction of the vector indicates the axis of rotation (using
      the right hand rule).

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

.. class:: VesselType

   .. data:: Ship

   .. data:: Station

   .. data:: Lander

   .. data:: Probe

   .. data:: Rover

   .. data:: Base

   .. data:: Debris

.. class:: VesselSituation

   .. data:: Docked

   .. data:: Escaping

   .. data:: Flying

   .. data:: Landed

   .. data:: Orbiting

   .. data:: PreLaunch

   .. data:: Splashed

   .. data:: SubOrbital

.. _Ferram Aerospace Research: http://forum.kerbalspaceprogram.com/threads/20451-0-90-Ferram-Aerospace-Research-v0-14-6-12-27-14
.. _RemoteTech: http://forum.kerbalspaceprogram.com/threads/83305-0-90-0-RemoteTech-v1-6-3-2015-02-06
.. _KSPs stock aerodynamic model: http://wiki.kerbalspaceprogram.com/wiki/Atmosphere
