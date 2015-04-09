CelestialBody
=============

.. class:: CelestialBody

   Represents a celestial body (such as a planet or moon).

   .. attribute:: Name

      Gets the name of the body.

      :rtype: string

   .. attribute:: Satellites

      Gets a list of celestial bodies that are in orbit around this celestial body.

      :rtype: :class:`List` ( :class:`CelestialBody` )

   .. attribute:: Mass

      Gets the mass of the body, in kilograms.

      :rtype: double

   .. attribute:: GravitationalParameter

      Gets the `standard gravitational parameter
      <http://en.wikipedia.org/wiki/Standard_gravitational_parameter>`_ of the body
      in :math:`m^3s^{-2}`.

      :rtype: double

   .. attribute:: SurfaceGravity

      Gets the acceleration due to gravity at sea level (mean altitude) on the
      body, in :math:`m/s^2`.

      :rtype: double

   .. attribute:: RotationalPeriod

      Gets the rotational period of the body, in seconds.

      :rtype: double

   .. attribute:: RotationalSpeed

      Returns the rotational speed of the body, in radians per second.

      :rtype: double

   .. attribute:: EquatorialRadius

      Gets the equatorial radius of the body, in meters.

      :rtype: double

   .. attribute:: SphereOfInfluence

      Gets the radius of the sphere of influence of the body, in meters.

      :rtype: double

   .. attribute:: Orbit

      Gets the orbit of the body.

      :rtype: :class:`Orbit`

   .. attribute:: HasAtmosphere

      `True` if the body has an atmosphere.

      :rtype: bool

   .. attribute:: AtmospherePressure

      Gets the pressure of the atmosphere at sea level, in Pascals. Returns 0 if
      the body has no atmosphere.

      :rtype: double

      .. note:: Calculated using `KSPs stock aerodynamic model`_.

   .. attribute:: AtmopshereDensity

      Gets the density of the atmosphere at sea level, in :math:`kg/m^3`. Returns 0
      if the body has no atmosphere.

      :rtype: double

      .. note:: Calculated using `KSPs stock aerodynamic model`_.

   .. attribute:: AtmosphereScaleHeight

      Gets the `scale height
      <http://wiki.kerbalspaceprogram.com/wiki/Kerbin#Atmosphere>`_ of the
      atmosphere, in meters. Returns 0 if the atmosphere has no atmosphere.

      :rtype: double

      .. note:: Calculated using `KSPs stock aerodynamic model`_.

   .. attribute:: AtmosphereMaxAltitude

      Gets the maximum altitude of the atmosphere, in meters. Returns 0 if the body
      has no atmosphere.

      :rtype: double

      .. note:: Calculated using `KSPs stock aerodynamic model`_.

   .. method:: AtmopsherePressureAt (altitude)

      Returns the atmospheric pressure, in Pascals, at the given altitude above sea
      level, in meters. Returns 0 if the body has no atmosphere.

      :param double altitude:
      :rtype: double

      .. note:: Calculated using `KSPs stock aerodynamic model`_.

   .. method:: AtmopshereDensityAt (altitude)

      Returns the density of the atmosphere, in :math:`kg/m^3`, at the given
      altitude above sea level, in meters. Returns 0 if the body has no atmosphere.

      :param double altitude:
      :rtype: double

      .. note:: Calculated using `KSPs stock aerodynamic model`_.

   .. attribute:: ReferenceFrame

      Gets the reference frame that is fixed relative to the celestial body.

      * The origin is at the center of the body.

      * The axes rotate with the body.

      * The x-axis points from the center of the body towards the intersection of
        the prime meridian and equator (the position at 0° longitude, 0° latitude).

      * The y-axis points from the center of the body towards the north pole.

      * The z-axis points from the center of the body towards the equator at 90°E longitude.

      :rtype: :class:`ReferenceFrame`

      .. figure:: /images/reference-frames/celestial-body.png
         :align: center

         Celestial body reference frame origin and axes. The equator is shown in
         blue, and the prime meridian in red.

   .. attribute:: NonRotatingReferenceFrame

      Gets the reference frame that is fixed relative to this celestial body, and
      orientated in a fixed direction (it does not rotate with the body).

      * The origin is at the center of the body.

      * The axes do not rotate.

      * The x-axis points in an arbitrary direction through the equator.

      * The y-axis points from the center of the body towards the north pole.

      * The z-axis points in an arbitrary direction through the equator.

      :rtype: :class:`ReferenceFrame`

   .. attribute:: OrbitalReferenceFrame

      Gets the reference frame that is fixed relative to this celestial body, but
      orientated with the body's orbital prograde/normal/radial directions.

      * The origin is at the center of the body.

      * The axes rotate with the orbital prograde/normal/radial directions.

      * The x-axis points in the orbital anti-radial direction.

      * The y-axis points in the orbital prograde direction.

      * The z-axis points in the orbital normal direction.

      :rtype: :class:`ReferenceFrame`

   .. method:: Position (referenceFrame)

      Returns the position vector of the center of the body in the specified reference frame.

      :param ReferenceFrame reference_frame:
      :rtype: :class:`Vector3`

   .. method:: Velocity (referenceFrame)

      Returns the velocity vector of the body in the specified reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

   .. method:: Rotation (referenceFrame)

      Returns the rotation of the body in the specified reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Quaternion`

   .. method:: Direction (referenceFrame)

      Returns the direction in which the north pole of the celestial body is
      pointing, as a unit vector, in the specified reference frame.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

   .. method:: AngularVelocity (referenceFrame)

      Returns the angular velocity of the body in the specified reference
      frame. The magnitude of the vector is the rotational speed of the body, in
      radians per second, and the direction of the vector indicates the axis of
      rotation, using the right-hand rule.

      :param ReferenceFrame referenceFrame:
      :rtype: :class:`Vector3`

.. _KSPs stock aerodynamic model: http://wiki.kerbalspaceprogram.com/wiki/Atmosphere
