Reference Frames
================

.. contents::
   :local:

Introduction
------------

All of the positions, directions and velocities in kRPC need to be relative to
something -- which is what a reference frame provides.

A reference frame is defined by an origin (the position ``(0,0,0)``) and a set
of axes (``x``, ``y``, and ``z``). The reference frame can also have a linear
velocity (the velocity of the origin) and an angular/rotational velocity which
specifies the speed at which the axes rotate.

.. note:: KSP and kRPC use a left handed coordinate system.

Origin Position and Axes Orientation
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The following gives some examples of the origin position and axes orientation
for some different reference frames.

.. figure:: /images/reference-frames/celestial-body.png
   :align: right
   :figwidth: 250

   The reference frame for a celestial body, such as Kerbin. The equator is
   shown in blue, and the prime meridian in red. The black arrows show the axes,
   and the origin is at the center of the planet.

The reference frame obtained by calling :attr:`CelestialBody.reference_frame`
for Kerbin has the following properties:

* The origin is at the center of Kerbin,

* the y-axis points from the center of Kerbin to the north pole,

* the x-axis points from the center of Kerbin to the intersection of the prime
  meridian and equator (the surface position at 0° longitude, 0° latitude),

* the z-axis points from the center of Kerbin to the equator at 90°E longitude,

* and the axes rotate with the planet, i.e. the reference frame has the same
  rotational/angular velocity as Kerbin.

.. container:: clearer

   ..

.. figure:: /images/reference-frames/vessel-orbital.png
   :align: right
   :figwidth: 350

   The orbital reference frame for a vessel.

Another example of a reference frame is the one obtained by calling
:attr:`Vessel.orbital_reference_frame` for the current vessel. This reference
frame is attached to the vessel (the origin moves with the vessel) but it is
orientated so that the axes point in the orbital prograde/normal/radial
directions:

* The origin is at the center of mass of the vessel,

* the y-axis points in the prograde direction of the vessels orbit,

* the x-axis points in the anti-radial direction of the vessels orbit,

* the z-axis points in the normal direction of the vessels orbit,

* and the axes rotate with any changes to the prograde/normal/radial directions,
  for example when the prograde direction changes as the vessel continues on its
  orbit.

.. container:: clearer

   ..

.. figure:: /images/reference-frames/vessel-aircraft.png
   :align: right
   :figwidth: 350

   The reference frame for an aircraft.

Compare this to the reference frame returned by calling
:attr:`Vessel.reference_frame`. This reference frame is also attached to the
vessel (the origin moves with the vessel), however its orientation is
different. The axes track the orientation of the vessel:

* The origin is at the center of mass of the vessel,

* the y-axis points in the same direction that the vessel is pointing,

* the x-axis and z-axis point out to the side of the vessel,

* and the axes rotate with any changes to the direction of the vessel.

.. container:: clearer

   ..

Linear Velocity and Angular Velocity
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Reference frames can move and rotate relative to one another. For example, the
reference frames discussed previously all have their origin position fixed to
some object (such as a vessel). This means they move and rotate to track said
object, and therefore have both a linear and angular velocity.

For example, the reference frame obtained by calling the
:attr:`CelestialBody.reference_frame` for Kerbin is fixed relative to
Kerbin. This means the angular velocity of the reference frame is identical to
Kerbin's angular velocity, and the linear velocity of the reference frame
matches the velocity of Kerbin through space.

Available Reference Frames
--------------------------

kRPC provides the following reference frames:

* :meth:`Vessel.reference_frame`
* :meth:`Vessel.orbital_reference_frame`
* :meth:`Vessel.surface_reference_frame`
* :meth:`Vessel.surface_velocity_reference_frame`
* :meth:`CelestialBody.reference_frame`
* :meth:`CelestialBody.non_rotating_reference_frame`
* :meth:`CelestialBody.orbital_reference_frame`
* :meth:`Node.reference_frame`
* :meth:`Node.orbital_reference_frame`

Converting Between Reference Frames
-----------------------------------

kRPC provides a few utility methods to convert positions, directions and
velocities between reference frames:

* :meth:`SpaceCenter.transform_position`
* :meth:`SpaceCenter.transform_direction`
* :meth:`SpaceCenter.transform_rotation`
* :meth:`SpaceCenter.transform_velocity`

Visual Debugging
----------------

:meth:`SpaceCenter.DrawDirection` can be used to draw a direction vector
in-game, and is useful for visualizing reference frames and debugging your
code. For example, the following will draw the vessels surface velocity vector
in red:

.. code-block:: python

   import krpc
   conn = krpc.connect(name='Navball directions')
   vessel = conn.space_center.active_vessel
   ref_frame = vessel.orbit.body.reference_frame

   velocity = vessel.flight(ref_frame).velocity
   conn.space_center.draw_direction(velocity, ref_frame, (1,0,0))

   while True:
      pass

.. note:: The client must remain connected, otherwise kRPC will stop drawing the
          directions, hence the while loop at the end of this example.

Examples
--------

The following examples demonstrate the use of reference frames.

Navball directions
^^^^^^^^^^^^^^^^^^

This example demonstrates how to make the vessel point in various directions on
the navball:

.. code-block:: python
   :linenos:

   import krpc
   conn = krpc.connect(name='Navball directions')
   vessel = conn.space_center.active_vessel

   # Point the vessel north on the navball, with a pitch of 0 degrees
   vessel.auto_pilot.set_direction((0,1,0), reference_frame=vessel.surface_reference_frame, wait=True)

   # Point the vessel vertically upwards on the navball
   vessel.auto_pilot.set_direction((1,0,0), reference_frame=vessel.surface_reference_frame, wait=True)

   # Point the vessel west (heading of 270 degrees), with a pitch of 0 degrees
   vessel.auto_pilot.set_direction((0,0,-1), reference_frame=vessel.surface_reference_frame, wait=True)

This code uses the vessel's surface reference frame
(:attr:`Vessel.surface_reference_frame`) pictured below:

.. image:: /images/reference-frames/vessel-surface.png
   :align: center

Line 6 instructs the auto-pilot to point in direction ``(0,1,0)`` (i.e. along
the y-axis) in the vessel's surface reference frame. The y-axis of the reference
frame points in the north direction, as required.

Line 9 instructs the auto-pilot to point in direction ``(1,0,0)`` (along the
x-axis) in the vessel's surface reference frame. This x-axis of the reference
frame points upwards (away from the planet) as required.

Line 12 instructs the auto-pilot to point in direction ``(0,0,-1)`` (along the
negative z axis). The z-axis of the reference frame points east, so the
requested direction points west -- as required.

Orbital directions
^^^^^^^^^^^^^^^^^^

This example demonstrates how to make the vessel point in the various orbital
directions, as seen on the navball when it is in 'orbit' mode, using the
:attr:`Vessel.orbital_reference_frame` reference frame.

.. code-block:: python
   :linenos:

   import krpc
   conn = krpc.connect(name='Orbital directions')
   vessel = conn.space_center.active_vessel

   # Point the vessel in the prograde direction
   vessel.auto_pilot.set_direction((0,1,0), reference_frame=vessel.orbital_reference_frame, wait=True)

   # Point the vessel in the orbit normal direction
   vessel.auto_pilot.set_direction((0,0,1), reference_frame=vessel.orbital_reference_frame, wait=True)

   # Point the vessel in the orbit radial direction
   vessel.auto_pilot.set_direction((-1,0,0), reference_frame=vessel.orbital_reference_frame, wait=True)

This code uses the vessel's orbital reference frame pictured below:

.. image:: /images/reference-frames/vessel-orbital.png
   :align: center

Surface 'prograde'
^^^^^^^^^^^^^^^^^^

This example demonstrates how to point the vessel in the 'prograde' direction on
the navball, when in surface mode. This is the direction of the velocity of the
vessel relative to the surface:

.. code-block:: python
   :linenos:

   import krpc
   conn = krpc.connect(name='Surface prograde')
   vessel = conn.space_center.active_vessel

   vessel.auto_pilot.set_direction((0,1,0), reference_frame=vessel.surface_velocity_reference_frame, wait=True)

This code uses the :attr:`Vessel.surface_velocity_reference_frame` pictured
below:

.. image:: /images/reference-frames/vessel-surface-velocity.png
   :align: center

Orbital speed
^^^^^^^^^^^^^

To compute the orbital speed of a vessel, you need to get the velocity relative
to the planet's non-rotating reference frame
(:attr:`CelestialBody.non_rotating_reference_frame`). This reference frame is
fixed relative to the body, but does not rotate:

.. code-block:: python
   :linenos:

   import krpc, time
   conn = krpc.connect(name='Orbital speed')
   vessel = conn.space_center.active_vessel

   while True:

       velocity = vessel.flight(vessel.orbit.body.non_rotating_reference_frame).velocity
       print 'Orbital velocity = (%.1f, %.1f, %.1f)' % velocity

       speed = vessel.flight(vessel.orbit.body.non_rotating_reference_frame).speed
       print 'Orbital speed = %.1f m/s' % speed

       time.sleep(1)

Surface speed
^^^^^^^^^^^^^

To compute the speed of a vessel relative to the surface of a planet/moon, you
need to get the velocity relative to the planets's reference frame
(:attr:`CelestialBody.reference_frame`). This reference frame rotates with the
body, therefore the rotational velocity of the body is taken into account when
computing the velocity of the vessel:

.. code-block:: python
   :linenos:

   import krpc, time
   conn = krpc.connect(name='Surface speed')
   vessel = conn.space_center.active_vessel

   while True:

       velocity = vessel.flight(vessel.orbit.body.reference_frame).velocity
       print 'Surface velocity = (%.1f, %.1f, %.1f)' % velocity

       speed = vessel.flight(vessel.orbit.body.reference_frame).speed
       print 'Surface speed = %.1f m/s' % speed

       time.sleep(1)

Angle of attack
^^^^^^^^^^^^^^^

This example computes the angle between the direction the vessel is pointing in,
and the direction that the vessel is moving in (relative to the surface):

.. code-block:: python
   :linenos:

   import krpc, math, time
   conn = krpc.connect(name='Angle of attack')
   vessel = conn.space_center.active_vessel

   while True:

       d = vessel.direction(vessel.orbit.body.reference_frame)
       v = vessel.velocity(vessel.orbit.body.reference_frame)

       # Compute the dot product of d and v
       dotprod = d[0]*v[0] + d[1]*v[1] + d[2]*v[2]

       # Compute the magnitude of v
       vmag = math.sqrt(v[0]**2 + v[1]**2 + v[2]**2)
       # Note: don't need to magnitude of d as it is a unit vector

       # Compute the angle between the vectors
       if dotprod == 0:
           angle = 0
       else:
           angle = abs(math.acos (dotprod / vmag) * (180. / math.pi))

       print 'Angle of attack = %.1f' % angle

       time.sleep(1)

The orientation of the reference frame used to get the direction and velocity
vectors (on lines 7 and 8) does not matter, as the angle between two vectors is
the same regardless of the orientation of the axes. However, if we were to use a
reference frame that moves with the vessel, line 8 would return ``(0,0,0)``. We
therefore need a reference frame that is not fixed relative to the
vessel. :attr:`CelestialBody.reference_frame` fits these requirements.
