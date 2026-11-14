.. _user-guide-vectors-and-rotations:

Vectors and Rotations
=====================

Positions, velocities and directions are vectors, and attitudes are rotations. Both are tuples of
numbers, so they work the same way in every client and every service. Each one is given in a
:ref:`reference frame <reference-frames>`, which sets its origin and axes.

Vectors
-------

A vector is a 3-tuple ``(x, y, z)``. This example reads the prograde direction of the active
vessel:

.. tabs::

   .. tab:: Python

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Vector3.py
         :language: python

   .. tab:: C#

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Vector3.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Vector3.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Vector3.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Vector3.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Vector3.lua
         :language: lua

   .. tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Vector3.js
         :language: javascript

A position is in meters and a velocity in meters per second. A direction is a unit vector.

.. _user-guide-handedness:

Handedness
----------

KSP and kRPC use a *left-handed* coordinate system, as Unity does. Point the thumb of your left
hand along the x axis and your index finger along the y axis. Your middle finger then points along
the z axis.

The cross product uses the usual formula, so the cross product of the x and y axes is the z axis.
The left-hand rule gives the direction of the result.

A positive rotation about an axis turns clockwise, seen from the tip of the axis looking back
toward the origin. Curl the fingers of your left hand with your thumb along the axis, and they
point in the direction of a positive rotation. So a rotation of +90° about the y axis takes the z
axis onto the x axis.

Rotations
---------

A rotation is a unit quaternion, a 4-tuple ``(x, y, z, w)``. The rotation that does nothing is
``(0, 0, 0, 1)``. This example reads the rotation of the active vessel:

.. tabs::

   .. tab:: Python

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Quaternion.py
         :language: python

   .. tab:: C#

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Quaternion.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Quaternion.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Quaternion.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Quaternion.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Quaternion.lua
         :language: lua

   .. tab:: JavaScript

      .. literalinclude:: /scripts/user-guide/vectors-and-rotations/Quaternion.js
         :language: javascript

The rotation of an object takes the axes of the reference frame onto the axes of the object. The y
axis of a vessel points out of its nose. So rotating ``(0, 1, 0)`` by ``Vessel.Rotation`` gives
``Vessel.Direction``, in the same reference frame.

Angles
------

Most angles are in degrees, such as pitch, heading and roll, and latitude and longitude. The
orbital elements of an ``Orbit``, the rotation angles of a ``CelestialBody`` and the angles of the
standard library are in radians. The API reference gives the unit of each one.
``StdLib.DegreesToRadians`` and ``StdLib.RadiansToDegrees`` convert between them.

Computing on the Server
-----------------------

The standard library service does vector and quaternion arithmetic, so you do not need a maths
library in your client. This example finds the angle between the direction of the active vessel
and its prograde direction:

.. literalinclude:: /scripts/user-guide/vectors-and-rotations/AngleToPrograde.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The vector functions add, subtract, scale, normalize and interpolate vectors, and give their dot
and cross products, lengths, distances and angles. The quaternion functions compose, invert and
interpolate rotations, rotate vectors, and build rotations from an axis and angle, between two
directions, or from a forward and up direction.

Each of these is a remote call. In a :ref:`server side function <server-side-functions>`, the
server evaluates them without a round trip, which makes them the way to work with vectors there.
