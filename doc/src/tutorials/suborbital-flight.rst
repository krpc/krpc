Sub-Orbital Flight
==================

This introductory tutorial uses kRPC to send some Kerbals on a sub-orbital
flight, and (hopefully) returns them safely back to Kerbin. It covers the
following topics:

* Controlling a rocket (activating stages, setting the throttle)
* Using the auto pilot to point the vessel in a specific direction
* Tracking the amount of resources in the vessel
* Tracking flight and orbital data (such as altitude and apoapsis altitude)

.. note:: For details on how to write scripts and connect to kRPC, see the
          :ref:`getting-started` guide.

Part One: Preparing for Launch
------------------------------

This tutorial uses the two stage rocket pictured below. The craft file for this
rocket can be :download:`downloaded here </crafts/SubOrbitalFlight.craft>` and
the entire python script for this tutorial :download:`from here
</scripts/SubOrbitalFlight.py>`

.. image:: /images/tutorials/SubOrbitalFlight.png
   :align: center

The first thing we need to do is load the python client module and open a
connection to the server. We can also pass a descriptive name for our script
that will appear in the server window in game:

.. code-block:: python

   import krpc
   conn = krpc.connect(name='Sub-orbital flight script')

Next we need to get an object representing the active vessel. It's via this
object that we will send instructions to the rocket:

.. code-block:: python

   vessel = conn.space_center.active_vessel

We then need to prepare the rocket for launch. The following code sets the
throttle to maximum and instructs the auto-pilot to hold a pitch and heading of
90° (vertically upwards). It then waits for 1 second for these settings to take
effect.

.. code-block:: python

   vessel.control.throttle = 1
   vessel.auto_pilot.set_rotation(90,90)
   import time
   time.sleep(1)

Part Two: Lift-off!
-------------------

We're now ready to launch by activating the first stage (equivalent to pressing
the space bar):

.. code-block:: python

   print('Launch!')
   vessel.control.activate_next_stage()

The rocket has a solid fuel stage that will quickly run out, and will need to be
jettisoned. We can monitor the amount of solid fuel in the rocket using a while
loop that repeatedly checks how much solid fuel there is left in the
rocket. When the loop exits, we will activate the next stage to jettison the
boosters:

.. code-block:: python

   while vessel.resources.amount('SolidFuel') > 0.1:
       time.sleep(1)
   print('Booster separation')
   control.activate_next_stage()

In this bit of code, ``vessel.resources`` returns a :class:`Resources` object
that is used to get information about the resources in the rocket.

Part Three: Reaching Apoapsis
-----------------------------

Next we will execute a gravity turn when the rocket reaches a sufficiently high
altitude. The following loop repeatedly checks the altitude and exits when the
rocket reaches 10km:

.. code-block:: python

   while vessel.flight().mean_altitude < 10000:
       time.sleep(1)

In this bit of code, calling ``vessel.flight()`` returns a :class:`Flight`
object that is used to get all sorts of information about the rocket, such as
the direction it is pointing in and its velocity.

Now we need to angle the rocket over to a pitch of 60° and maintain a heading of
90° (west). To do this, we simply reconfigure the auto-pilot:

.. code-block:: python

   print('Gravity turn')
   vessel.auto_pilot.set_rotation(60,90)

Now we wait until the apoapsis reaches 100km, then reduce the throttle to zero,
jettison the launch stage and turn off the auto-pilot:

.. code-block:: python

   while vessel.orbit.apoapsis_altitude < 100000:
       time.sleep(1)

   print('Launch stage separation')
   vessel.control.throttle = 0
   time.sleep(1)
   vessel.control.activate_next_stage()
   vessel.auto_pilot.disengage()

In this bit of code, ``vessel.orbit`` returns an :class:`Orbit` object that
contains all the information about the orbit of the rocket.

Part Four: Returning Safely to Kerbin
-------------------------------------

Our Kerbals are now heading on a sub-orbital trajectory and are on a collision
course with the surface. All that remains to do is wait until they fall to 1km
altitude above the surface, and then deploy the parachutes. If you like, you can
use time acceleration to skip ahead to just before this happens - the script
will continue to work.

.. code-block:: python

   while vessel.flight().surface_altitude > 1000:
       time.sleep(1)
   vessel.control.activate_next_stage()

The parachutes should have now been deployed. The next bit of code will
repeatedly print out the altitude of the capsule until its speed reaches zero --
which will happen when it lands:

.. code-block:: python

   while vessel.flight(vessel.orbit.body.reference_frame).vertical_speed < -0.1:
       print('Altitude = %.1f meters' % vessel.flight().surface_altitude)
       time.sleep(1)
   print('Landed!')

This bit of code uses the ``vessel.flight()`` function, as before, but this time
it is passed a :class:`ReferenceFrame` parameter. We want to get the vertical
speed of the capsule relative to the surface of Kerbin, so the values returned
by the flight object need to be relative to the surface of Kerbin. We therefore
pass ``vessel.orbit.body.reference_frame`` to ``vessel.flight()`` as this
reference frame has its origin at the center of Kerbin and it rotates with the
planet. For more information, check out the tutorial on
:ref:`tutorial-reference-frames`.

Your Kerbals should now have safely landed back on the surface.
