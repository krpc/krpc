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

.. literalinclude:: /scripts/SubOrbitalFlight.py
   :lines: 1-2

Next we need to get an object representing the active vessel. It's via this
object that we will send instructions to the rocket:

.. literalinclude:: /scripts/SubOrbitalFlight.py
   :lines: 4

We then need to prepare the rocket for launch. The following code sets the
throttle to maximum and instructs the auto-pilot to hold a pitch and heading of
90° (vertically upwards). It then waits for 1 second for these settings to take
effect.

.. literalinclude:: /scripts/SubOrbitalFlight.py
   :lines: 6-10

Part Two: Lift-off!
-------------------

We're now ready to launch by activating the first stage (equivalent to pressing
the space bar):

.. literalinclude:: /scripts/SubOrbitalFlight.py
   :lines: 12-13

The rocket has a solid fuel stage that will quickly run out, and will need to be
jettisoned. We can monitor the amount of solid fuel in the rocket using a while
loop that repeatedly checks how much solid fuel there is left in the
rocket. When the loop exits, we will activate the next stage to jettison the
boosters:

.. literalinclude:: /scripts/SubOrbitalFlight.py
   :lines: 15-18

In this bit of code, ``vessel.resources`` returns a :class:`Resources` object
that is used to get information about the resources in the rocket.

Part Three: Reaching Apoapsis
-----------------------------

Next we will execute a gravity turn when the rocket reaches a sufficiently high
altitude. The following loop repeatedly checks the altitude and exits when the
rocket reaches 10km:

.. literalinclude:: /scripts/SubOrbitalFlight.py
   :lines: 20-21

In this bit of code, calling ``vessel.flight()`` returns a :class:`Flight`
object that is used to get all sorts of information about the rocket, such as
the direction it is pointing in and its velocity.

Now we need to angle the rocket over to a pitch of 60° and maintain a heading of
90° (west). To do this, we simply reconfigure the auto-pilot:

.. literalinclude:: /scripts/SubOrbitalFlight.py
   :lines: 23-24

Now we wait until the apoapsis reaches 100km, then reduce the throttle to zero,
jettison the launch stage and turn off the auto-pilot:

.. literalinclude:: /scripts/SubOrbitalFlight.py
   :lines: 26-32

In this bit of code, ``vessel.orbit`` returns an :class:`Orbit` object that
contains all the information about the orbit of the rocket.

Part Four: Returning Safely to Kerbin
-------------------------------------

Our Kerbals are now heading on a sub-orbital trajectory and are on a collision
course with the surface. All that remains to do is wait until they fall to 1km
altitude above the surface, and then deploy the parachutes. If you like, you can
use time acceleration to skip ahead to just before this happens - the script
will continue to work.

.. literalinclude:: /scripts/SubOrbitalFlight.py
   :lines: 34-36

The parachutes should have now been deployed. The next bit of code will
repeatedly print out the altitude of the capsule until its speed reaches zero --
which will happen when it lands:

.. literalinclude:: /scripts/SubOrbitalFlight.py
   :lines: 38-41

This bit of code uses the ``vessel.flight()`` function, as before, but this time
it is passed a :class:`ReferenceFrame` parameter. We want to get the vertical
speed of the capsule relative to the surface of Kerbin, so the values returned
by the flight object need to be relative to the surface of Kerbin. We therefore
pass ``vessel.orbit.body.reference_frame`` to ``vessel.flight()`` as this
reference frame has its origin at the center of Kerbin and it rotates with the
planet. For more information, check out the tutorial on
:ref:`tutorial-reference-frames`.

Your Kerbals should now have safely landed back on the surface.
