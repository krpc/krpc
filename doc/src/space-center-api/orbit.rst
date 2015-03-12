Orbit
=====

.. class:: Orbit

   Describes an orbit. For example, the orbit of a vessel (:meth:`Vessel.Orbit`)
   or celestial body (:meth:`CelestialBody.Orbit`).

.. attribute:: Orbit.Body

   Gets the celestial body (e.g. planet or moon) around which the object is orbiting.

   :rtype: :class:`CelestialBody`

.. attribute:: Orbit.Apoapsis

   Gets the apoapsis of the orbit, in meters, from the center of mass of the body being orbited.

   .. note:: For the apoapsis altitude reported on the in-game map view, use
      :attr:`Orbit.apoapsis_altitude`.

   :rtype: `double`

.. attribute:: Orbit.Periapsis

   Gets the periapsis of the orbit, in meters, from the center of mass of the body being orbited.

   :rtype: `double`

   .. note:: For the periapsis altitude reported on the in-game map view, use
      :attr:`Orbit.periapsis_altitude`.

.. attribute:: Orbit.ApoapsisAltitude

   Gets the apoapsis of the orbit, in meters, above sea level of the body being
   orbited.

   :rtype: `double`

   .. note:: This is equal to :attr:`Orbit.apoapsis` minus the equatorial radius
      of the body.

.. attribute:: Orbit.PeriapsisAltitude

   Gets the periapsis of the orbit, in meters, above sea level of the body being
   orbited.

   :rtype: `double`

   .. note:: This is equal to :attr:`Orbit.periapsis` minus the equatorial
      radius of the body.

.. attribute:: Orbit.SemiMajorAxis

   Gets the semi-major axis of the orbit, in meters.

   :rtype: `double`

.. attribute:: Orbit.SemiMinorAxis

   Gets the semi-minor axis of the orbit, in meters.

   :rtype: `double`

.. attribute:: Orbit.Radius

   Gets the current radius of the orbit, in meters. This is the distance between
   the center of mass of the object in orbit, and the center of mass of the body
   around which it is orbiting.

   :rtype: `double`

   .. note:: This value will change over time if the orbit is elliptical.

.. attribute:: Orbit.Speed

   Gets the current orbital speed of the object in meters per second.

   :rtype: `double`

   .. note:: This value will change over time if the orbit is elliptical.

.. attribute:: Orbit.TimeToApoapsis

   Gets the time until the object reaches apoapsis, in seconds.

   :rtype: `double`

.. attribute:: Orbit.TimeToPeriapsis

   Gets the time until the object reaches periapsis, in seconds.

   :rtype: `double`

.. attribute:: Orbit.TimeToSOIChange

   Gets the time until the object changes sphere of influence, in
   seconds. Returns `NaN` if the object is not going to change sphere of
   influence.

   :rtype: `double`

.. attribute:: Orbit.Eccentricity

   Gets the `eccentricity <http://en.wikipedia.org/wiki/Orbital_eccentricity>`_
   of the orbit.

   :rtype: `double`

.. attribute:: Orbit.Inclination

   Gets the `inclination <http://en.wikipedia.org/wiki/Orbital_inclination>`_ of
   the orbit, in radians.

   :rtype: `double`

.. attribute:: Orbit.LongitudeOfAscendingNode

   Gets the `longitude of the ascending node
   <http://en.wikipedia.org/wiki/Longitude_of_the_ascending_node>`_, in radians.

   :rtype: `double`

.. attribute:: Orbit.ArgumentOfPeriapsis

   Gets the `argument of periapsis
   <http://en.wikipedia.org/wiki/Argument_of_periapsis>`_, in radians.

   :rtype: `double`

.. attribute:: _Orbit_ Orbit.NextOrbit

   If the object is going to change sphere of influence in the future, returns
   the new orbit after the change. Otherwise returns `null`.

   :rtype: :class:`Orbit`

.. attribute:: Orbit.Period

   Gets the orbital period, in seconds.

   :rtype: `double`

.. attribute:: Orbit.MeanAnomaly

   Gets the `mean anomaly <http://en.wikipedia.org/wiki/Mean_anomaly>`_.

   :rtype: `double`

.. attribute:: Orbit.EccentricAnomaly

   Gets the `eccentric anomaly <http://en.wikipedia.org/wiki/Eccentric_anomaly>`_.

   :rtype: `double`

.. attribute:: Orbit.MeanAnomalyAtEpoch

   Gets the `mean anomaly at epoch
   <http://en.wikipedia.org/wiki/Mean_anomaly>`_.

   :rtype: `double`

.. attribute:: Orbit.Epoch

   Gets the time since the epoch (the point at which the `mean anomaly at epoch
   <http://en.wikipedia.org/wiki/Mean_anomaly>`_ was measured, in seconds.

   :rtype: `double`

.. attribute:: Orbit.ReferencePlaneNormal

   Gets the unit direction vector that is normal to the orbits reference
   plane. The reference plane is the plane from which the orbits inclination is
   measured.

   :rtype: :class:`Vector`

.. attribute:: Orbit.ReferencePlaneDirection

   Gets the unit direction vector (in the reference plane) from which the orbits
   longitude of ascending node is measured.

   :rtype: :class:`Vector`

.. attribute:: Orbit.ReferenceFrame

   Gets the reference frame for the orbit.
   The origin is at the position of the object in orbit.
   The x-axis points in the north direction of the body being orbited.
   The y-axis points normal to the body being orbited (from the center of the
   body to the object in orbit).

   :rtype: :class:`ReferenceFrame`
