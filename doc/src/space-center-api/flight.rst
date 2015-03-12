Flight
======

.. class:: Flight

   Used to get flight telemetry for a vessel, by calling
   :meth:`Vessel.Flight`. All of the information returned by this class is given
   in the reference frame passed to that method.

   .. note:: To get orbital information, such as the apoapsis or inclination,
             see :class:`Orbit`.

.. attribute:: Flight.GForce

   Gets the current G force acting on the vessel in :math:`m/s^2`.

   :rtype: double

.. attribute:: Flight.MeanAltitude

   Gets the altitude above sea level, in meters.

   :rtype: double

.. attribute:: Flight.SurfaceAltitude

   Gets the altitude above the surface of the body or sea level (whichever is
   closer), in meters.

   :rtype: double

.. attribute:: Flight.BedrockAltitude

   Gets the altitude above the surface of the body, in meters. When over water,
   this is the altitude above the sea floor.

   :rtype: double

.. attribute:: Flight.Elevation

   Gets the elevation of the terrain under the vessel, in meters. This is the
   height of the terrain above sea level, and is negative when the vessel is
   over the sea.

   :rtype: double

.. attribute:: Flight.Velocity

   Gets the velocity vector of the vessel. The magnitude of the vector is the
   speed of the vessel in meters per second. The direction of the vector is the
   direction of the vessels motion.

   :rtype: :class:`Vector`

.. attribute:: Flight.Speed

   Gets the speed of the vessel in meters per second.

   :rtype: double

.. attribute:: Flight.HorizontalSpeed

   Gets the horizontal speed of the vessel in meters per second.

   :rtype: double

.. attribute:: Flight.VerticalSpeed

   Gets the vertical speed of the vessel in meters per second.

   :rtype: double

.. attribute:: Flight.CenterOfMass

   Gets the position of the center of mass of the vessel.

   :rtype: :class:`Vector`

.. attribute:: Flight.Drag

   Gets the aerodynamic drag currently acting on the vessel in :math:`kg.m/s^2`.

   :rtype: double

   .. note::

      This is calculated using `KSPs basic aerodynamic model
      <http://wiki.kerbalspaceprogram.com/wiki/Atmosphere>`_.

.. attribute:: Flight.direction

   Gets the direction vector that the vessel is pointing in.

   :rtype: :class:`Vector`

.. attribute:: Flight.Rotation

   Gets the rotation of the vessel.

   :rtype: :class:`Quaternion`

.. attribute:: Flight.Pitch

   Gets the pitch angle of the vessel relative to the horizon, in degrees. A
   value between -90° and +90°.

   :rtype: double

.. attribute:: Flight.Heading

   Gets the heading angle of the vessel relative to north, in degrees. A value
   between 0° and 360°.

   :rtype: double

.. attribute:: Flight.Roll

   Gets the roll angle of the vessel relative to the horizon, in degrees. A
   value between -180° and +180°.

   :rtype: double

.. attribute:: Flight.prograde

   Gets the unit direction vector pointing in the prograde direction.

   :rtype: :class:`Vector`

.. attribute:: Flight.normal

   Gets a unit direction vector pointing in the normal direction.

   :rtype: :class:`Vector`

.. attribute:: Flight.radial

   Gets a unit direction vector pointing in the radial direction direction.

   :rtype: :class:`Vector`

Examples
--------

Getting the orbital speed of a vessel
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

.. code-block:: python

   conn = krpc.connect()
   vessel = conn.space_center.active_vessel
   flight = vessel.flight()
   orbital_speed = flight.speed

Getting the vertical speed of a vessel relative to the surface
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

.. code-block:: python

   conn = krpc.connect()
   vessel = conn.space_center.active_vessel
   flight = vessel.flight(vessel.SurfaceReferenceFrame)
   surface_speed = flight.vertical_speed
