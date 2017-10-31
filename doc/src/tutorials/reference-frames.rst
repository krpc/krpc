.. csharp:namespace:: KRPC.Client.Services.SpaceCenter
.. cpp:namespace:: krpc::services::SpaceCenter
.. java:package:: krpc.client.services.SpaceCenter
.. lua:currentmodule:: SpaceCenter
.. py:currentmodule:: SpaceCenter

.. _tutorial-reference-frames:

Reference Frames
================

.. contents::
   :local:

Introduction
------------

All of the positions, directions, velocities and rotations in kRPC are relative
to something, and *reference frames* define what that something is.

A reference frame specifies:

* The position of the origin at (0,0,0)
* the direction of the coordinate axes x, y, and z
* the linear velocity of the origin (if the reference frame moves)
* The angular velocity of the coordinate axes (the speed and direction of rotation of the axes)

.. note:: KSP and kRPC use a left handed coordinate system

Origin Position and Axis Orientation
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The following gives some examples of the position of the origin and the
orientation of the coordinate axes for various reference frames.

Celestial Body Reference Frame
""""""""""""""""""""""""""""""

.. figure:: /images/reference-frames/celestial-body.png
   :align: right
   :figwidth: 250

   The reference frame for a celestial body, such as Kerbin. The equator is
   shown in blue, and the prime meridian in red. The black arrows show the
   coordinate axes, and the origin is at the center of the planet.

The reference frame obtained by calling :attr:`CelestialBody.reference_frame`
for Kerbin has the following properties:

* The origin is at the center of Kerbin,

* the y-axis points from the center of Kerbin to the north pole,

* the x-axis points from the center of Kerbin to the intersection of the prime
  meridian and equator (the surface position at 0° longitude, 0° latitude),

* the z-axis points from the center of Kerbin to the equator at 90°E longitude,

* and the axes rotate with the planet, i.e. the reference frame has the same
  rotational/angular velocity as Kerbin.

This means that the reference frame is *fixed* relative to Kerbin -- it moves
with the center of the planet, and also rotates with the planet. Therefore,
positions in this reference frame are relative to the center of the planet. The
following code prints out the position of the active vessel in Kerbin's
reference frame:

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselPosition.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselPosition.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselPosition.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselPosition.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselPosition.lua
         :language: lua

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselPosition.py
         :language: python

For a vessel sat on the launchpad, the magnitude of this position vector will be
roughly 600,000 meters (equal to the radius of Kerbin). The position vector will
also not change over time, because the vessel is sat on the surface of Kerbin
and the reference frame also rotates with Kerbin.

Vessel Orbital Reference Frame
""""""""""""""""""""""""""""""

.. figure:: /images/reference-frames/vessel-orbital.png
   :align: right
   :figwidth: 350

   The orbital reference frame for a vessel.

Another example is the orbital reference frame for a vessel, obtained by calling
:attr:`Vessel.orbital_reference_frame`. This is fixed to the vessel (the origin
moves with the vessel) and is orientated so that the axes point in the orbital
prograde/normal/radial directions.

* The origin is at the center of mass of the vessel,

* the y-axis points in the prograde direction of the vessels orbit,

* the x-axis points in the anti-radial direction of the vessels orbit,

* the z-axis points in the normal direction of the vessels orbit,

* and the axes rotate to match any changes to the prograde/normal/radial directions,
  for example when the prograde direction changes as the vessel continues on its
  orbit.

Vessel Surface Reference Frame
""""""""""""""""""""""""""""""

.. figure:: /images/reference-frames/vessel-aircraft.png
   :align: right
   :figwidth: 350

   The reference frame for an aircraft.

Another example is :attr:`Vessel.reference_frame`. As with the previous example,
it is fixed to the vessel (the origin moves with the vessel), however the
orientation of the coordinate axes is different. They track the orientation of
the vessel:

* The origin is at the center of mass of the vessel,

* the y-axis points in the same direction that the vessel is pointing,

* the x-axis points out of the right side of the vessel,

* the z-axis points downwards out of the bottom of the vessel,

* and the axes rotate with any changes to the direction of the vessel.

Linear Velocity and Angular Velocity
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Reference frames move and rotate relative to one another. For example, the
reference frames discussed previously all have their origin position fixed to
some object (such as a vessel or a planet). This means that they move and rotate
to track the object, and so have a linear and angular velocity associated with
them.

For example, the reference frame obtained by calling
:attr:`CelestialBody.reference_frame` for Kerbin is fixed relative to
Kerbin. This means the angular velocity of the reference frame is identical to
Kerbin's angular velocity, and the linear velocity of the reference frame
matches the current orbital velocity of Kerbin.

Available Reference Frames
--------------------------

kRPC provides the following reference frames:

.. tabs::

   .. tab:: C#

      * :csharp:prop:`Vessel.ReferenceFrame`
      * :csharp:prop:`Vessel.OrbitalReferenceFrame`
      * :csharp:prop:`Vessel.SurfaceReferenceFrame`
      * :csharp:prop:`Vessel.SurfaceVelocityReferenceFrame`
      * :csharp:prop:`CelestialBody.ReferenceFrame`
      * :csharp:prop:`CelestialBody.NonRotatingReferenceFrame`
      * :csharp:prop:`CelestialBody.OrbitalReferenceFrame`
      * :csharp:prop:`Node.ReferenceFrame`
      * :csharp:prop:`Node.OrbitalReferenceFrame`
      * :csharp:prop:`Part.ReferenceFrame`
      * :csharp:prop:`Part.CenterOfMassReferenceFrame`
      * :csharp:prop:`DockingPort.ReferenceFrame`
      * :csharp:prop:`Thruster.ThrustReferenceFrame`

   .. tab:: C++

      * :cpp:func:`Vessel::reference_frame`
      * :cpp:func:`Vessel::orbital_reference_frame`
      * :cpp:func:`Vessel::surface_reference_frame`
      * :cpp:func:`Vessel::surface_velocity_reference_frame`
      * :cpp:func:`CelestialBody::reference_frame`
      * :cpp:func:`CelestialBody::non_rotating_reference_frame`
      * :cpp:func:`CelestialBody::orbital_reference_frame`
      * :cpp:func:`Node::reference_frame`
      * :cpp:func:`Node::orbital_reference_frame`
      * :cpp:func:`Part::reference_frame`
      * :cpp:func:`Part::center_of_mass_reference_frame`
      * :cpp:func:`DockingPort::reference_frame`
      * :cpp:func:`Thruster::thrust_reference_frame`

   .. tab:: C

      * :c:func:`krpc_SpaceCenter_Vessel_ReferenceFrame`
      * :c:func:`krpc_SpaceCenter_Vessel_OrbitalReferenceFrame`
      * :c:func:`krpc_SpaceCenter_Vessel_SurfaceReferenceFrame`
      * :c:func:`krpc_SpaceCenter_Vessel_SurfaceVelocityReferenceFrame`
      * :c:func:`krpc_SpaceCenter_CelestialBody_ReferenceFrame`
      * :c:func:`krpc_SpaceCenter_CelestialBody_NonRotatingReferenceFrame`
      * :c:func:`krpc_SpaceCenter_CelestialBody_OrbitalReferenceFrame`
      * :c:func:`krpc_SpaceCenter_Node_ReferenceFrame`
      * :c:func:`krpc_SpaceCenter_Node_OrbitalReferenceFrame`
      * :c:func:`krpc_SpaceCenter_Part_ReferenceFrame`
      * :c:func:`krpc_SpaceCenter_Part_CenterOfMassReferenceFrame`
      * :c:func:`krpc_SpaceCenter_DockingPort_ReferenceFrame`
      * :c:func:`krpc_SpaceCenter_Thruster_ThrustReferenceFrame`

   .. tab:: Java

      * :java:meth:`Vessel.getReferenceFrame`
      * :java:meth:`Vessel.getOrbitalReferenceFrame`
      * :java:meth:`Vessel.getSurfaceReferenceFrame`
      * :java:meth:`Vessel.getSurfaceVelocityReferenceFrame`
      * :java:meth:`CelestialBody.getReferenceFrame`
      * :java:meth:`CelestialBody.getNonRotatingReferenceFrame`
      * :java:meth:`CelestialBody.getOrbitalReferenceFrame`
      * :java:meth:`Node.getReferenceFrame`
      * :java:meth:`Node.getOrbitalReferenceFrame`
      * :java:meth:`Part.getReferenceFrame`
      * :java:meth:`Part.getCenterOfMassReferenceFrame`
      * :java:meth:`DockingPort.getReferenceFrame`
      * :java:meth:`Thruster.getThrustReferenceFrame`

   .. tab:: Lua

      * :lua:attr:`Vessel.reference_frame`
      * :lua:attr:`Vessel.orbital_reference_frame`
      * :lua:attr:`Vessel.surface_reference_frame`
      * :lua:attr:`Vessel.surface_velocity_reference_frame`
      * :lua:attr:`CelestialBody.reference_frame`
      * :lua:attr:`CelestialBody.non_rotating_reference_frame`
      * :lua:attr:`CelestialBody.orbital_reference_frame`
      * :lua:attr:`Node.reference_frame`
      * :lua:attr:`Node.orbital_reference_frame`
      * :lua:attr:`Part.reference_frame`
      * :lua:attr:`Part.center_of_mass_reference_frame`
      * :lua:attr:`DockingPort.reference_frame`
      * :lua:attr:`Thruster.thrust_reference_frame`

   .. tab:: Python

      * :py:attr:`Vessel.reference_frame`
      * :py:attr:`Vessel.orbital_reference_frame`
      * :py:attr:`Vessel.surface_reference_frame`
      * :py:attr:`Vessel.surface_velocity_reference_frame`
      * :py:attr:`CelestialBody.reference_frame`
      * :py:attr:`CelestialBody.non_rotating_reference_frame`
      * :py:attr:`CelestialBody.orbital_reference_frame`
      * :py:attr:`Node.reference_frame`
      * :py:attr:`Node.orbital_reference_frame`
      * :py:attr:`Part.reference_frame`
      * :py:attr:`Part.center_of_mass_reference_frame`
      * :py:attr:`DockingPort.reference_frame`
      * :py:attr:`Thruster.thrust_reference_frame`

Relative and hybrid reference frames can also be constructed from the above.

Custom Reference Frames
-----------------------

Custom reference frames can be constructed from the built in frames listed
above. They come in two varieties: 'relative' and 'hybrid'.

A relative reference frame is constructed from a parent reference frame, a fixed
position offset and a fixed rotation offset. For example, this could be used to
construct a reference frame whose origin is 10m below the vessel as follows, by
applying a position offset of 10 along the z-axis to
:attr:`Vessel.reference_frame`. Relative reference frames can be constructed by
calling :meth:`ReferenceFrame.create_relative`.

A hybrid reference frame inherits its components (position, rotation, velocity
and angular velocity) from the components of other reference frames. Note that
these components need not be fixed. For example, you could construct a reference
frame whose position is the center of mass of the vessel (inherited from
:attr:`Vessel.reference_frame`) and whose rotation is that of the planet being
orbited (inherited from :attr:`CelestialBody.reference_frame`). Relative
reference frames can be constructed by calling
:meth:`ReferenceFrame.create_hybrid`.

The parent reference frame(s) of a custom reference frame can also be other
custom reference frames. For example, you could combine the two example frames
from above: construct a hybrid reference frame, centered on the vessel and
rotated with the planet being orbited, and then create a relative reference that
offsets the position of this 10m along the z-axis. The resulting frame will have
its origin 10m below the vessel, and will be rotated with the planet being
orbited.

Converting Between Reference Frames
-----------------------------------

kRPC provides utility methods to convert positions, directions, rotations and
velocities between the different reference frames:


.. tabs::

   .. tab:: C#

      * :csharp:meth:`SpaceCenter.TransformPosition`
      * :csharp:meth:`SpaceCenter.TransformDirection`
      * :csharp:meth:`SpaceCenter.TransformRotation`
      * :csharp:meth:`SpaceCenter.TransformVelocity`

   .. tab:: C++

      * :cpp:func:`SpaceCenter::transform_position`
      * :cpp:func:`SpaceCenter::transform_direction`
      * :cpp:func:`SpaceCenter::transform_rotation`
      * :cpp:func:`SpaceCenter::transform_velocity`

   .. tab:: C

      * :c:func:`krpc_SpaceCenter_TransformPosition`
      * :c:func:`krpc_SpaceCenter_TransformDirection`
      * :c:func:`krpc_SpaceCenter_TransformRotation`
      * :c:func:`krpc_SpaceCenter_TransformVelocity`

   .. tab:: Java

      * :java:meth:`SpaceCenter.transformPosition`
      * :java:meth:`SpaceCenter.transformDirection`
      * :java:meth:`SpaceCenter.transformRotation`
      * :java:meth:`SpaceCenter.transformVelocity`

   .. tab:: Lua

      * :lua:meth:`SpaceCenter.transform_position`
      * :lua:meth:`SpaceCenter.transform_direction`
      * :lua:meth:`SpaceCenter.transform_rotation`
      * :lua:meth:`SpaceCenter.transform_velocity`

   .. tab:: Python

      * :py:meth:`SpaceCenter.transform_position`
      * :py:meth:`SpaceCenter.transform_direction`
      * :py:meth:`SpaceCenter.transform_rotation`
      * :py:meth:`SpaceCenter.transform_velocity`

Visual Debugging
----------------

References frames can be confusing, and choosing the correct one is a challenge
in itself. To aid debugging, kRPCs drawing functionality can be used to
visualize direction vectors in-game.

:meth:`Drawing.add_direction` will draw a direction vector, starting from the
origin of the given reference frame. For example, the following code draws the
direction of the current vessels velocity relative to the surface of the body it
is orbiting:

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/reference-frames/VisualDebugging.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/reference-frames/VisualDebugging.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/reference-frames/VisualDebugging.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/reference-frames/VisualDebugging.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/reference-frames/VisualDebugging.lua
         :language: lua

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/reference-frames/VisualDebugging.py
         :language: python

.. note:: The client must remain connected for the line to continue to be drawn,
          hence the infinite loop at the end of this example.

Examples
--------

The following examples demonstrate various uses of reference frames.

Navball directions
^^^^^^^^^^^^^^^^^^

This example demonstrates how to make the vessel point in various directions on
the navball:

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/reference-frames/NavballDirections.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/reference-frames/NavballDirections.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/reference-frames/NavballDirections.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/reference-frames/NavballDirections.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/reference-frames/NavballDirections.lua
         :language: lua

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/reference-frames/NavballDirections.py
         :language: python

The code uses the vessel's surface reference frame
(:attr:`Vessel.surface_reference_frame`), pictured below:

.. image:: /images/reference-frames/vessel-surface.png
   :align: center

The first part instructs the auto-pilot to point in direction ``(0,1,0)``
(i.e. along the y-axis) in the vessel's surface reference frame. The y-axis of
the reference frame points in the north direction, as required.

The second part instructs the auto-pilot to point in direction ``(1,0,0)``
(along the x-axis) in the vessel's surface reference frame. This x-axis of the
reference frame points upwards (away from the planet) as required.

Finally, the code instructs the auto-pilot to point in direction ``(0,0,-1)``
(along the negative z axis). The z-axis of the reference frame points east, so
the requested direction points west -- as required.

Orbital directions
^^^^^^^^^^^^^^^^^^

This example demonstrates how to make the vessel point in the various orbital
directions, as seen on the navball when it is in 'orbit' mode. It uses
:attr:`Vessel.orbital_reference_frame`.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/reference-frames/OrbitalDirections.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/reference-frames/OrbitalDirections.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/reference-frames/OrbitalDirections.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/reference-frames/OrbitalDirections.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/reference-frames/OrbitalDirections.lua
         :language: lua

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/reference-frames/OrbitalDirections.py
         :language: python

This code uses the vessel's orbital reference frame, pictured below:

.. image:: /images/reference-frames/vessel-orbital.png
   :align: center

Surface 'prograde'
^^^^^^^^^^^^^^^^^^

This example demonstrates how to point the vessel in the 'prograde' direction on
the navball, when in 'surface' mode. This is the direction of the vessels
velocity relative to the surface:

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/reference-frames/SurfacePrograde.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/reference-frames/SurfacePrograde.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/reference-frames/SurfacePrograde.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/reference-frames/SurfacePrograde.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/reference-frames/SurfacePrograde.lua
         :language: lua

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/reference-frames/SurfacePrograde.py
         :language: python

This code uses the :attr:`Vessel.surface_velocity_reference_frame`, pictured
below:

.. image:: /images/reference-frames/vessel-surface-velocity.png
   :align: center

.. _tutorial-reference-frames-vessel-speed:

Vessel Speed
^^^^^^^^^^^^

This example demonstrates how to get the orbital and surface speeds of the
vessel, equivalent to the values displayed by the navball.

To compute the orbital speed of a vessel, you need to get the velocity relative
to the planet's *non-rotating* reference frame
(:attr:`CelestialBody.non_rotating_reference_frame`). This reference frame is
fixed relative to the body, but does not rotate.

For the surface speed, the planet's reference frame
(:attr:`CelestialBody.reference_frame`) is required, as this reference frame
rotates with the body.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselSpeed.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselSpeed.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselSpeed.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselSpeed.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselSpeed.lua
         :language: lua

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselSpeed.py
         :language: python

.. _tutorial-reference-frames-vessel-velocity:

Vessel Velocity
^^^^^^^^^^^^^^^

This example demonstrates how to get the velocity of the vessel (as a vector),
relative to the surface of the body being orbited.

To do this, a hybrid reference frame is required. This is because we want a
reference frame that is centered on the vessel, but whose linear velocity is
fixed relative to the ground.

We therefore create a hybrid reference frame with its rotation set to the
vessel's surface reference frame (:attr:`Vessel.surface_reference_frame`), and
all other properties (including position and velocity) set to the body's
reference frame (:attr:`CelestialBody.reference_frame`) -- which rotates with
the body.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselVelocity.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselVelocity.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselVelocity.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselVelocity.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselVelocity.lua
         :language: lua

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/reference-frames/VesselVelocity.py
         :language: python

Angle of attack
^^^^^^^^^^^^^^^

This example computes the angle between the direction the vessel is pointing in,
and the direction that the vessel is moving in (relative to the surface):

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/reference-frames/AngleOfAttack.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/reference-frames/AngleOfAttack.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/reference-frames/AngleOfAttack.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/reference-frames/AngleOfAttack.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/reference-frames/AngleOfAttack.lua
         :language: lua

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/reference-frames/AngleOfAttack.py
         :language: python

Note that the orientation of the reference frame used to get the direction and
velocity vectors does not matter, as the angle between two vectors is the same
regardless of the orientation of the axes. However, if we were to use a
reference frame that moves with the vessel, the velocity would return
``(0,0,0)``. We therefore need a reference frame that is not fixed relative to
the vessel. :attr:`CelestialBody.reference_frame` fits these requirements.

Landing Site
^^^^^^^^^^^^

This example computes a reference frame that is located on the surface of a body
at a given altitude, which could be used as the target for a landing auto pilot.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/tutorials/reference-frames/LandingSite.cs
         :language: csharp

   .. tab:: C++

      .. literalinclude:: /scripts/tutorials/reference-frames/LandingSite.cpp
         :language: cpp

   .. tab:: C

      .. literalinclude:: /scripts/tutorials/reference-frames/LandingSite.c
         :language: c

   .. tab:: Java

      .. literalinclude:: /scripts/tutorials/reference-frames/LandingSite.java
         :language: java

   .. tab:: Lua

      .. literalinclude:: /scripts/tutorials/reference-frames/LandingSite.lua
         :language: lua

   .. tab:: Python

      .. literalinclude:: /scripts/tutorials/reference-frames/LandingSite.py
         :language: python
