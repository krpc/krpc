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

      :rtype: float

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

      :rtype: float

   .. attribute:: Heading

      Gets the heading angle of the vessel relative to north, in degrees. A value
      between 0° and 360°.

      :rtype: float

   .. attribute:: Roll

      Gets the roll angle of the vessel relative to the horizon, in degrees. A
      value between -180° and +180°.

      :rtype: float

   .. attribute:: Prograde

      Gets the unit direction vector pointing in the prograde direction.

      :rtype: :class:`Vector3`

   .. attribute:: Retrograde

      Gets the unit direction vector pointing in the retrograde direction.

      :rtype: :class:`Vector3`

   .. attribute:: Normal

      Gets a unit direction vector pointing in the normal direction.

      :rtype: :class:`Vector3`

   .. attribute:: AntiNormal

      Gets a unit direction vector pointing in the anti-normal direction.

      :rtype: :class:`Vector3`

   .. attribute:: Radial

      Gets a unit direction vector pointing in the radial direction.

      :rtype: :class:`Vector3`

   .. attribute:: AntiRadial

      Gets a unit direction vector pointing in the anti-radial direction.

      :rtype: :class:`Vector3`

   .. attribute:: AtmosphereDensity

      Gets the current density of the atmosphere around the vessel, in
      :math:`kg/m^3`.

      :rtype: float

      .. note:: Calculated using `KSPs stock aerodynamic model`_, or `Ferram
         Aerospace Research`_ if it is installed.

   .. attribute:: DynamicPressure

      Gets the dynamic pressure acting on the vessel, in Pascals. This is a
      measure of the strength of the aerodynamic forces. It is equal to
      :math:`\frac{1}{2} . \mbox{air density} .  \mbox{velocity}^2`. It is
      commonly denoted as :math:`Q`.

      :rtype: float

      .. note:: Calculated using `KSPs stock aerodynamic model`_, or `Ferram
         Aerospace Research`_ if it is installed.

   .. attribute:: StaticPressure

      Gets the static atmospheric pressure acting on the vessel, in Pascals.

      :rtype: float

      .. note:: Calculated using `KSPs stock aerodynamic model`_. Not available
         when `Ferram Aerospace Research`_ if it is installed.

   .. attribute:: AerodynamicForce

      Gets the total aerodynamic forces acting on the vessel, as a vector
      pointing in the direction of the force, with its magnitude equal to the
      strength of the force in Newtons.

      :rtype: float

      .. note:: Calculated using `KSPs stock aerodynamic model`_. Not available
         when `Ferram Aerospace Research`_ if it is installed.

   .. attribute:: Lift

      Gets the `aerodynamic lift
      <http://en.wikipedia.org/wiki/Aerodynamic_force>`_ currently acting on the
      vessel, as a vector pointing in the direction of the force, with its
      magnitude equal to the strength of the force in Newtons.

      :rtype: :class:`Vector3`

      .. note:: Calculated using `KSPs stock aerodynamic model`_. Not available
         when `Ferram Aerospace Research`_ if it is installed

   .. attribute:: Drag

      Gets the `aerodynamic drag
      <http://en.wikipedia.org/wiki/Aerodynamic_force>`_ currently acting on the
      vessel, as a vector pointing in the direction of the force, with its
      magnitude equal to the strength of the force in Newtons.

      :rtype: :class:`Vector3`

      .. note:: Calculated using `KSPs stock aerodynamic model`_. Not available
         when `Ferram Aerospace Research`_ if it is installed

   .. attribute:: SpeedOfSound

      The speed of sound, in the atmosphere around the vessel, in :math:`m/s`.

      :rtype: float

      .. note:: Not available when `Ferram Aerospace Research`_ if it is
         installed.

   .. attribute:: Mach

      The speed of the vessel, in multiples of the speed of sound.

      :rtype: float

      .. note:: Calculated using `KSPs stock aerodynamic model`_, or `Ferram
         Aerospace Research`_ if it is installed.

   .. attribute:: EquivalentAirSpeed

      The `equivalent air speed
      <http://en.wikipedia.org/wiki/Equivalent_airspeed>`_ of the vessel, in
      :math:`m/s`.

      :rtype: float

      .. note:: Not available when `Ferram Aerospace Research`_ if it is
         installed.

   .. attribute:: TerminalVelocity

      The current terminal velocity of the vessel, in :math:`m/s`. This is the
      speed at which the drag forces cancel out the force of gravity.

      :rtype: float

      .. note:: Calculated using `KSPs stock aerodynamic model`_, or `Ferram
         Aerospace Research`_ if it is installed.

   .. attribute:: AngleOfAttack

      Gets the pitch angle between the orientation of the vessel and its
      velocity vector, in degrees.

      :rtype: float

   .. attribute:: SideslipAngle

      Gets the yaw angle between the orientation of the vessel and its velocity
      vector, in degrees.

      :rtype: float

   .. attribute:: TotalAirTemperature

      The `total air temperature
      <http://en.wikipedia.org/wiki/Total_air_temperature>`_ of the atmosphere
      around the vessel, in Kelvin. This temperature includes the
      :attr:`StaticAirTemperature` and the vessel's kinetic energy.

      :rtype: float

   .. attribute:: StaticAirTemperature

      The `static (ambient) temperature
      <http://en.wikipedia.org/wiki/Total_air_temperature>`_ of the atmosphere
      around the vessel, in Kelvin.

      :rtype: float

   .. attribute:: StallFraction

      Gets the current amount of stall, between 0 and 1. A value greater than 0.005
      indicates a minor stall and a value greater than 0.5 indicates a large-scale
      stall.

      :rtype: float

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: DragCoefficient

      Gets the coefficient of drag. This is the amount of drag produced by the
      vessel. When calculated using `Ferram Aerospace Research`_ it depends on air
      speed, air density and wing area.

      :rtype: float

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: LiftCoefficient

      Gets the coefficient of lift. This is the amount of lift produced by the
      vessel, and depends on air speed, air density and wing area.

      :rtype: float

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: PitchingMomentCoefficient

      Gets the `pitching moment coefficient
      <http://en.wikipedia.org/wiki/Pitching_moment#Coefficient>`_.

      :rtype: float

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: BallisticCoefficient

      Gets the `ballistic coefficient
      <http://en.wikipedia.org/wiki/Ballistic_coefficient>`_.

      :rtype: float

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: ThrustSpecificFuelConsumption

      Gets the thrust specific fuel consumption for the jet engines on the
      vessel. This is a measure of the efficiency of the engines, with a lower
      value indicating a more efficient vessel. This value is the number of Newtons
      of fuel that are burned, per hour, to product one newton of thrust.

      :rtype: float

      .. note:: Requires `Ferram Aerospace Research`_

   .. attribute:: FARStatus

      Gets current status message from `Ferram Aerospace Research`_.

      :rtype: string

      .. note:: Requires `Ferram Aerospace Research`_

.. _Ferram Aerospace Research: http://forum.kerbalspaceprogram.com/threads/20451-0-90-Ferram-Aerospace-Research-v0-14-6-12-27-14
.. _KSPs stock aerodynamic model: http://wiki.kerbalspaceprogram.com/wiki/Atmosphere
