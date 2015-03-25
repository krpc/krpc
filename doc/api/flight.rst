Flight
======

.. class:: Flight

   Used to get flight telemetry for a vessel, by calling
   :meth:`Vessel.Flight`. All of the information returned by this class is given
   in the reference frame passed to that method.

   .. note:: To get orbital information, such as the apoapsis or inclination,
             see :class:`Orbit`.

   .. attribute:: GForce

      Gets the current G force acting on the vessel in :math:`m/s^2`.

      :rtype: double

   .. attribute:: MeanAltitude

      Gets the altitude above sea level, in meters.

      :rtype: double

   .. attribute:: SurfaceAltitude

      Gets the altitude above the surface of the body or sea level (whichever is
      closer), in meters.

      :rtype: double

   .. attribute:: BedrockAltitude

      Gets the altitude above the surface of the body, in meters. When over water,
      this is the altitude above the sea floor.

      :rtype: double

   .. attribute:: Elevation

      Gets the elevation of the terrain under the vessel, in meters. This is the
      height of the terrain above sea level, and is negative when the vessel is
      over the sea.

      :rtype: double

   .. attribute:: Latitude

      Gets the `latitude <http://en.wikipedia.org/wiki/Latitude>`_ of the vessel
      for the body being orbited, in degrees.

      :rtype: double

   .. attribute:: Longitude

      Gets the `longitude <http://en.wikipedia.org/wiki/Longitude>`_ of the vessel
      for the body being orbited, in degrees.

      :rtype: double

   .. attribute:: Velocity

      Gets the velocity vector of the vessel. The magnitude of the vector is the
      speed of the vessel in meters per second. The direction of the vector is the
      direction of the vessels motion.

      :rtype: :class:`Vector3`

   .. attribute:: Speed

      Gets the speed of the vessel in meters per second.

      :rtype: double

   .. attribute:: HorizontalSpeed

      Gets the horizontal speed of the vessel in meters per second.

      :rtype: double

   .. attribute:: VerticalSpeed

      Gets the vertical speed of the vessel in meters per second.

      :rtype: double

   .. attribute:: CenterOfMass

      Gets the position of the center of mass of the vessel.

      :rtype: :class:`Vector3`

   .. attribute:: Rotation

      Gets the rotation of the vessel.

      :rtype: :class:`Quaternion`

   .. attribute:: Direction

      Gets the direction vector that the vessel is pointing in.

      :rtype: :class:`Vector3`

   .. attribute:: Pitch

      Gets the pitch angle of the vessel relative to the horizon, in degrees. A
      value between -90° and +90°.

      :rtype: double

   .. attribute:: Heading

      Gets the heading angle of the vessel relative to north, in degrees. A value
      between 0° and 360°.

      :rtype: double

   .. attribute:: Roll

      Gets the roll angle of the vessel relative to the horizon, in degrees. A
      value between -180° and +180°.

      :rtype: double

   .. attribute:: prograde

      Gets the unit direction vector pointing in the prograde direction.

      :rtype: :class:`Vector3`

   .. attribute:: normal

      Gets a unit direction vector pointing in the normal direction.

      :rtype: :class:`Vector3`

   .. attribute:: radial

      Gets a unit direction vector pointing in the radial direction direction.

      :rtype: :class:`Vector3`

   .. attribute:: AtmosphereDensity

      Gets the current density of the atmosphere around the vessel, in
      :math:`kg/m^3`.

      :rtype: `double`

      .. note:: Calculated using `Ferram Aerospace Research`_ if it is
         installed. Otherwise, calculated using `KSPs stock aerodynamic model`_

   .. attribute:: Drag

      Gets the aerodynamic drag force currently acting on the vessel, in Newtons.

      :rtype: double

   .. attribute:: DynamicPressure

      Gets the dynamic pressure acting on the vessel. This is a measure of the
      strength of the aerodynamic forces. It is equal to :math:`\frac{1}{2}
      . \mbox{air density} .  \mbox{velocity}^2`, and is measured in :math:`kg .
      m^{-1}s^{-2}`. It is commonly denoted as :math:`Q`.

      :rtype: double

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: AngleOfAttack

      Gets the pitch angle between the orientation of the vessel and its velocity
      vector, in degrees. (The angle between the mean chord of the wing and the
      free-stream velocity.)

      :rtype: double

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: SideslipAngle

      Gets the yaw angle between the orientation of the vessel and its velocity
      vector, in degrees. (The angle between the center line of the aircraft or
      rocket and the free-stream velocity in the lateral plane.)

      :rtype: double

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: StallFraction

      Gets the current amount of stall, between 0 and 1. A value greater than 0.005
      indicates a minor stall and a value greater than 0.5 indicates a large-scale
      stall.

      :rtype: double

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: MachNumber

      Gets the current mach number for the vessel. This is the current velocity
      divided by the local speed of sound.

      :rtype: double

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: TerminalVelocity

      Gets the terminal velocity of the vessel, in :math:`m/s`. This is the speed
      at which the drag forces cancel out the force of gravity.

      :rtype: double

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: DragCoefficient

      Gets the coefficient of drag. This is the amount of drag produced by the
      vessel. When calculated using `Ferram Aerospace Research`_ it depends on air
      speed, air density and wing area.

      :rtype: double

      .. note:: Calculated using `Ferram Aerospace Research`_ if it is
         installed. Otherwise, calculated using `KSPs stock aerodynamic model`_

   .. attribute:: LiftCoefficient

      Gets the coefficient of lift. This is the amount of lift produced by the
      vessel, and depends on air speed, air density and wing area.

      :rtype: double

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: PitchingMomentCoefficient

      Gets the `pitching moment coefficient
      <http://en.wikipedia.org/wiki/Pitching_moment#Coefficient>`_.

      :rtype: double

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: BallisticCoefficient

      Gets the `ballistic coefficient
      <http://en.wikipedia.org/wiki/Ballistic_coefficient>`_.

      :rtype: double

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: ThrustSpecificFuelConsumption

      Gets the thrust specific fuel consumption for the jet engines on the
      vessel. This is a measure of the efficiency of the engines, with a lower
      value indicating a more efficient vessel. This value is the number of Newtons
      of fuel that are burned, per hour, to product one newton of thrust.

      :rtype: double

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: FARStatus

      Gets current status message from `Ferram Aerospace Research`_.

      :rtype: string

      .. note:: Requires `Ferram Aerospace Research`_

.. _Ferram Aerospace Research: http://forum.kerbalspaceprogram.com/threads/20451-0-90-Ferram-Aerospace-Research-v0-14-6-12-27-14
.. _KSPs stock aerodynamic model: http://wiki.kerbalspaceprogram.com/wiki/Atmosphere
