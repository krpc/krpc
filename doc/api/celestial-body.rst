CelestialBody
=============

.. class:: CelestialBody

   Represents a celestial body (such as a planet or moon).

.. attribute:: CelestialBody.Name

   Gets the name of the body.

   :rtype: `string`

.. attribute:: CelestialBody.Satellites

   Gets a list of celestial bodies that are in orbit around this celestial body.

   :rtype: :class:`List` ( :class:`CelestialBody` )

.. attribute:: CelestialBody.Mass

   Gets the mass of the body, in kilograms.

   :rtype: `double`

.. attribute:: CelestialBody.GravitationalParameter

   Gets the `standard gravitational parameter
   <http://en.wikipedia.org/wiki/Standard_gravitational_parameter>`_ of the body
   in :math:`m^3s^{-2}`.

   :rtype: `double`

.. attribute:: CelestialBody.SurfaceGravity

   Gets the acceleration due to gravity at sea level (mean altitude) on the
   body, in :math:`m/s^2`.

   :rtype: `double`

.. attribute:: CelestialBody.RotationalPeriod

   Gets the rotational period of the body, in seconds.

   :rtype: `double`

.. attribute:: CelestialBody.RotationalSpeed

   Returns the rotational speed of the body, in radians per second.

   :rtype: `double`

.. attribute:: CelestialBody.EquatorialRadius

   Gets the equatorial radius of the body, in meters.

   :rtype: `double`

.. attribute:: CelestialBody.SphereOfInfluence

   Gets the radius of the sphere of influence of the body, in meters.

   :rtype: `double`

.. attribute:: CelestialBody.Orbit

   Gets the orbit of the body.

   :rtype: :class:`Orbit`

.. attribute:: CelestialBody.HasAtmosphere

   `True` if the body has an atmosphere.

   :rtype: `bool`

.. attribute:: CelestialBody.AtmospherePressure

   Gets the pressure of the atmosphere at sea level, in Pascals. Returns 0 if
   the body has no atmosphere.

   :rtype: `double`

.. attribute:: CelestialBody.AtmopshereDensity

   Gets the density of the atmosphere at sea level, in :math:`kg/m^3`. Returns 0
   if the body has no atmosphere.

   :rtype: `double`

.. attribute:: CelestialBody.AtmosphereScaleHeight

   Gets the `scale height
   <http://wiki.kerbalspaceprogram.com/wiki/Kerbin#Atmosphere>`_ of the
   atmosphere, in meters. Returns 0 if the atmosphere has no atmosphere.

   :rtype: `double`

.. attribute:: CelestialBody.AtmosphereMaxAltitude

   Gets the maximum altitude of the atmosphere, in meters. Returns 0 if the body
   has no atmosphere.

   :rtype: `double`

.. method:: CelestialBody.AtmopsherePressureAt (altitude)

   Returns the atmospheric pressure, in Pascals, at the given altitude above sea
   level, in meters. Returns 0 if the body has no atmosphere.

   :param double altitude:
   :rtype: `double`

.. method:: CelestialBody.AtmopshereDensityAt (altitude)

   Returns the density of the atmosphere, in :math:`kg/m^3`, at the given
   altitude above sea level, in meters. Returns 0 if the body has no atmosphere.

   :param double altitude:
   :rtype: `double`

.. attribute:: CelestialBody.ReferenceFrame

   Gets the reference frame that is fixed relative to this celestial body.
   The origin is at the center of the body.
   The y-axis points from the center of the body towards the north pole.
   The x-axis points from the center of the body towards the intersection of the
   prime meridian and equator (the position at 0 degrees longitude, 0 degrees
   latitude).

   :rtype: :class:`ReferenceFrame`

.. attribute:: CelestialBody.NonRotatingReferenceFrame

   Gets the reference frame whose origin is at the center of the body, and whose
   axes point in an arbitrary but fixed direction.

   :rtype: :class:`ReferenceFrame`

.. attribute:: CelestialBody.OrbitalReferenceFrame

   Gets the reference frame relative to the orbit of this body.
   The origin is at the center of the body.
   The x-axis points normal to the body being orbited (from the center of the
   body being orbited towards the center of this body).
   The y-axis points towards the north pole of the body being orbited.

   :rtype: :class:`ReferenceFrame`

.. attribute:: CelestialBody.SurfaceReferenceFrame

   Gets the reference frame relative to the surface of the body being orbited by
   this vessel.
   The origin is at the center of the body.
   The x-axis points normal to the body being orbited (from the center of the
   body being orbited towards the center of this body).
   The y-axis points towards the north pole of the body being orbited.

   :rtype: :class:`ReferenceFrame`

.. method:: CelestialBody.Position (referenceFrame)

   Returns the position vector of the center of the body in the specified reference frame.

   :param ReferenceFrame reference_frame:
   :rtype: :class:`Vector3`

.. method:: CelestialBody.Velocity (referenceFrame)

   Returns the velocity vector of the body in the specified reference frame.

   :param ReferenceFrame referenceFrame:
   :rtype: :class:`Vector3`

.. method:: CelestialBody.Rotation (referenceFrame)

   Returns the rotation of the body in the specified reference frame.

   :param ReferenceFrame referenceFrame:
   :rtype: :class:`Quaternion`

.. method:: CelestialBody.Direction (referenceFrame)

   Returns the direction in which the north pole of the celestial body is
   pointing, as a unit vector, in the specified reference frame.

   :param ReferenceFrame referenceFrame:
   :rtype: :class:`Vector3`

.. method:: CelestialBody.AngularVelocity (referenceFrame)

   Returns the angular velocity of the body in the specified reference
   frame. The magnitude of the vector is the rotational speed of the body, in
   radians per second, and the direction of the vector indicates the axis of
   rotation, using the right-hand rule.

   :param ReferenceFrame referenceFrame:
   :rtype: :class:`Vector3`
