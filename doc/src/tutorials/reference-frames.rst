Reference Frames
================

All of the positions, directions, velocities, rotations etc. need to be relative
to something -- which is where reference frames come in.

A reference frame is defined by an origin (the position ``(0,0,0)``) and a set
of axes (``x``, ``y``, and ``z``). The reference frame can also have a linear
velocity (the velocity of the origin) and an angular/rotational velocity which
specifies the speed at which the axes rotate.

.. note:: KSP, and therefore kRPC and its reference frames, uses a left handed
          coordinate system.

.. figure:: /images/tutorials/celestial-body-reference-frame.*
   :align: right
   :figwidth: 250

   The reference frame for a celestial body, such as Kerbin. The equator is
   shown in blue, and the prime meridian in red. The black arrows show the axes,
   and the origin is at the center of the planet.

For example, the reference frame obtained by calling
:attr:`CelestialBody.reference_frame` for Kerbin has the following properties:

* The origin is at the center of Kerbin,

* the y-axis points from the center of Kerbin to the north pole,

* the x-axis points from the center of Kerbin to the intersection of the prime
  meridian and equator (the surface position at 0° longitude, 0° latitude),

* the z-axis points from the center of Kerbin to the equator at 90°E longitude,

* and the axes rotate with the planet, i.e. the reference frame has the same
  rotational/angular velocity as Kerbin.

.. container:: clearer

   ..

.. figure:: /images/tutorials/vessel-orbital-reference-frame.*
   :align: right
   :figwidth: 350

   The orbital reference frame for a vessel. The black arrows show the axes,
   aligned with the orbital directions, and the origin is at the center of mass
   of the vessel.

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

Compare this to the reference frame returned by calling
:attr:`Vessel.reference_frame`. This reference frame is also attached to the
vessel (the origin moves with the vessel), however its orientation is
different. The axes track the orientation of the vessel:

* The origin is at the center of mass of the vessel,

* the y-axis points in the same direction that the vessel is pointing,

* the x-axis and z-axis point out to the side of the vessel,

* and the axes rotate with any changes to the direction of the vessel.

Navball directions
------------------

This example demonstrates how to make the vessel point in various directions on
the navball:

.. code-block:: python
   :linenos:

   import krpc
   conn = krpc.connect()
   vessel = conn.space_center.active_vessel

   # Point the vessel north on the navball, with a pitch of 0 degrees
   vessel.auto_pilot.set_direction((0,1,0), reference_frame = vessel.surface_reference_frame)
   while vessel.auto_pilot.error > 0.1:
       pass

   # Point the vessel vertically upwards on the navball
   vessel.auto_pilot.set_direction((1,0,0), reference_frame = vessel.surface_reference_frame)
   while vessel.auto_pilot.error > 0.1:
       pass

   # Point the vessel west (heading of 270 degrees), with a pitch of 0 degrees
   vessel.auto_pilot.set_direction((0,0,-1), reference_frame = vessel.surface_reference_frame)
   while vessel.auto_pilot.error > 0.1:
       pass

Line 6 instructs the auto-pilot to point in direction ``(0,1,0)`` (i.e. along
the y-axis) in the vessel's surface reference frame
(:attr:`Vessel.surface_reference_frame`). The y-axis of the reference frame
points in the north direction, as required.

Line 11 instructs the auto-pilot to point in direction ``(1,0,0)`` (along the
x-axis) in the vessel's surface reference frame. This x-axis of the reference
frame points upwards (away from the planet) as required.

Line 16 instructs the auto-pilot to point in direction ``(0,0,-1)`` (along the
negative z axis). The z-axis of the reference frame points east, so the
requested direction points west -- as required.

Orbital directions
------------------

This example demonstrates how to make the vessel point in the various orbital
directions, as seen on the navball when it is in 'orbit' mode, using the
:attr:`Vessel.orbital_reference_frame` reference frame.

.. code-block:: python
   :linenos:

   import krpc
   conn = krpc.connect()
   vessel = conn.space_center.active_vessel

   # Point the vessel in the prograde direction
   vessel.auto_pilot.set_direction((0,1,0), reference_frame = vessel.orbital_reference_frame)
   while vessel.auto_pilot.error > 0.1:
       pass

   # Point the vessel in the orbit normal direction
   vessel.auto_pilot.set_direction((0,0,1), reference_frame = vessel.orbital_reference_frame)
   while vessel.auto_pilot.error > 0.1:
       pass

   # Point the vessel in the orbit radial direction
   vessel.auto_pilot.set_direction((-1,0,0), reference_frame = vessel.orbital_reference_frame)
   while vessel.auto_pilot.error > 0.1:
       pass

Surface speed
-------------

To compute the speed of a vessel relative to the surface of a planet/moon, you
need to get the velocity relative to the planets's reference frame using
:attr:`CelestialBody.reference_frame`. This reference frame rotates with the
body, therefore the rotational velocity of the body is taken into account when
computing the velocity of the vessel:

.. code-block:: python
   :linenos:

   import krpc, time
   conn = krpc.connect()
   vessel = conn.space_center.active_vessel

   while True:

       velocity = vessel.flight(vessel.orbit.body.reference_frame).velocity
       print 'Surface velocity = (%.1f, %.1f, %.1f)' % v

       speed = vessel.flight(vessel.orbit.body.reference_frame).speed
       print 'Surface speed = %.1f m/s' % speed

       time.sleep(1)

Surface 'prograde'
------------------

This example demonstrates how to point the vessel in the 'prograde' direction on
the navball, when in surface mode. This is the direction of the velocity of the
vessel relative to the surface. We therefore need to use
:attr:`CelestialBody.reference_frame` as this reference frame rotates with the
planet. In other words, we want to point the vessel in the direction of the
velocity vector we got in the previous example:

.. code-block:: python
   :linenos:

   import krpc
   conn = krpc.connect()
   vessel = conn.space_center.active_vessel

   velocity = vessel.flight(vessel.orbit.body.reference_frame).velocity

   vessel.auto_pilot.set_direction(velocity, reference_frame = vessel.orbit.body.reference_frame)
   while vessel.auto_pilot.error > 0.1:
       pass

.. note:: The velocity passed to :meth:`AutoPilot.set_direction` does not need
          to be normalized to a unit vector.

.. note:: The reason I put 'prograde' in quotes here is that prograde refers to
          an orbital direction, whereas here we mean the direction of the
          velocity vector of the vessel relative to the surface.

Angle of attack
---------------

This example computes the angle between the direction the vessel is pointing in,
and the direction that the vessel is moving in (relative to the surface).

.. code-block:: python

   import krpc, math, time
   conn = krpc.connect()
   vessel = conn.space_center.active_vessel

   while True:

       d = vessel.direction(vessel.orbit.body.reference_frame)
       v = vessel.velocity(vessel.orbit.body.reference_frame)

       # Compute the dot product of d and v
       dotprod = d[0]*v[0] + d[1]*v[1] + d[2]*v[2]

       # Compute the angle between the vectors
       angle = abs(math.acos (dotprod) * (180. / math.pi))

       print 'Angle of attack = %.1f' % angle

       time.sleep(1)

Pitch and yaw angles
--------------------

The following example calculates the pitch and yaw angles of the vessel and
prints them out once per second:

.. code-block:: python

   import krpc, math, time
   conn = krpc.connect(name='Pitch/Yaw')
   vessel = conn.space_center.active_vessel

   while True:

       # x north
       # y normal to surface
       # z forward

       direction = vessel.direction(vessel.orbit.body.reference_frame)
       velocity = vessel.velocity(vessel.orbit.body.reference_frame)

       # x yaw
       # y pitch
       # z roll

       # direction unit vector, with corrected axes
       x0 = direction[1]
       y0 = direction[0]
       z0 = direction[2]

       # calculate unit vector of velocity
       magnitude = math.sqrt( velocity[0]**2 + velocity[1]**2 + velocity[2]**2 )
       v = (velocity[0]/ magnitude, velocity[1]/ magnitude, velocity[2]/ magnitude)

       # velocity unit vector, with corrected axes
       x1 = v[1]
       y1 = v[0]
       z1 = v[2]

       # calculate pitch
       pitch = math.acos((x0*x1 + y0*y1) / (math.sqrt(x0**2 + y0**2) * math.sqrt(x1**2 + y1 **2))) * (180 / math.pi)
       # pitch is positive if velocity is below attitude, negative if above (this is arbitrary)
       if x0 > x1:
           pitch = -pitch

       # calculate yaw
       yaw = math.acos((y0*y1 + z0*z1) / (math.sqrt(y0**2 + z0**2) * math.sqrt(y1**2 + z1 **2))) * (180 / math.pi)
       # yaw is positive if velocity is left of attitude, negative if right (this is arbitrary)
       if y0 < y1:
           yaw = -yaw

       print "pitch: % 5.1f, yaw: % 5.1f" % (pitch, yaw)

       time.sleep(1)
