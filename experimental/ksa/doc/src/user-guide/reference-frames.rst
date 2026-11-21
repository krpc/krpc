Reference Frames
================

Positions, velocities, directions and rotations are given in a reference frame. Vehicles and
bodies provide the frames, and every one is right-handed. This script gets the active
vehicle's velocity relative to the surface, and relative to the stars:

.. literalinclude:: /scripts/user-guide/reference-frames/Velocities.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The Frames
----------

.. list-table::
   :header-rows: 1

   * - Frame
     - Origin
     - Axes
   * - ``Body.ReferenceFrame``
     - The body's center
     - Fixed to the body: z to the north pole, x to latitude and longitude zero, y to latitude
       zero and longitude 90 degrees east.
   * - ``Body.NonRotatingReferenceFrame``
     - The body's center
     - z to the north pole, x and y fixed relative to the stars.
   * - ``Vehicle.ReferenceFrame``
     - The vehicle
     - Fixed to the vehicle's control point: x forward, y right, z down.
   * - ``Vehicle.SurfaceReferenceFrame``
     - The vehicle
     - The surface below it: x east, y north, z up. Rotates with the body.
   * - ``Vehicle.OrbitalReferenceFrame``
     - The vehicle
     - Its orbit: x prograde, y along the orbit normal, z radially outward.

Positions are in meters, velocities in meters per second and angular velocities in radians per
second. A rotation is a quaternion ``(x, y, z, w)``.

Directions and Attitude
-----------------------

``Vehicle.Direction`` is the unit vector the vehicle points in. In the surface frame, its pitch
and heading are those ``Flight`` reports:

.. literalinclude:: /scripts/user-guide/reference-frames/Direction.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

Pitch is positive above the horizon, and heading is clockwise from north. Roll is zero with
the vehicle's z axis toward the ground.

Converting Between Frames
-------------------------

``TransformPosition``, ``TransformDirection``, ``TransformRotation`` and ``TransformVelocity``
convert a vector or rotation from one frame to another:

.. literalinclude:: /scripts/user-guide/reference-frames/Transform.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

A velocity depends on the position it is measured at in a rotating frame, so
``TransformVelocity`` also takes the position. Frames convert through the game's ecliptic
frame, which every vehicle and body can convert to.
