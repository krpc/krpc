.. currentmodule:: SpaceCenter

Sub-Orbital Flight
==================

This introductory tutorial uses kRPC to send some Kerbals on a sub-orbital flight, and (hopefully)
returns them safely back to Kerbin. It covers the following topics:

* Controlling a rocket (activating stages, setting the throttle)
* Using the auto pilot to point the vessel in a specific direction
* Waiting for things to happen in game
* Tracking the amount of resources in the vessel
* Tracking flight and orbital data (such as altitude and apoapsis altitude)

.. note:: For details on how to write scripts and connect to kRPC, see the :ref:`getting-started`
          guide.

This tutorial uses the two stage rocket pictured below. The craft file for this rocket can be
:download:`downloaded here </crafts/SubOrbitalFlight.craft>`.

This tutorial includes source code examples for the main client languages that kRPC supports. The
entire program, for your chosen language can be downloaded from here:

:download:`C</scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.c>`,
:download:`C#</scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs>`,
:download:`C++</scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp>`,
:download:`Java</scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java>`,
:download:`Lua</scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua>`,
:download:`Python</scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py>`

.. image:: /images/tutorials/SubOrbitalFlight.png
   :align: center

Part One: Preparing for Launch
------------------------------

The first thing we need to do is open a connection to the server. We can also pass a descriptive
name for our script that will appear in the server window in game:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 7-9

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 9

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 8-9

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 16-17

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 1-3

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 4

Next we need to get an object representing the active vessel. It's via this object that we will send
instructions to the rocket:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 11-18

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 11

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 11

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 19

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 5

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 6

We then need to prepare the rocket for launch. The following code sets the throttle to maximum and
instructs the auto-pilot to hold a pitch and heading of 90° (vertically upwards). It then waits for
1 second for these settings to take effect.

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 20-23

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 13-16

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 13-16

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 21-24

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 7-10

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 8-11

Part Two: Lift-off!
-------------------

We're now ready to launch by activating the first stage (equivalent to pressing the space bar):

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 25-26

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 18-19

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 18-19

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 26-27

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 12-13

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 13-14

The rocket has a solid fuel stage that will quickly run out, and will need to be jettisoned. We can
check the amount of solid fuel in the rocket once a second, until there is very little left. We then
activate the next stage to jettison the boosters:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 28-38

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 21-25

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 21-25

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 29-34

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 15-19

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 16-19

In this bit of code, ``vessel.resources`` returns a :class:`Resources` object that is used to get
information about the resources in the rocket.

Part Three: Reaching Apoapsis
-----------------------------

Next we will execute a gravity turn when the rocket reaches a sufficiently high altitude. The
following waits until the altitude of the rocket reaches 10km:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 40-47

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 27-28

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 27-28

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 36-38

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 21-23

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 21-22

In this bit of code, calling ``vessel.flight()`` returns a :class:`Flight` object that is used to
get all sorts of information about the rocket, such as the direction it is pointing in and its
velocity.

Now we need to angle the rocket over to a pitch of 60° and maintain a heading of 90° (east). To do
this, we simply reconfigure the auto-pilot:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 49-50

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 30-31

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 30-31

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 40-41

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 25-26

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 24-25

Now we wait until the apoapsis reaches 100km, then reduce the throttle to
zero, jettison the launch stage and turn off the auto-pilot:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 52-65

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 33-40

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 33-40

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 43-51

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 28-35

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 27-34

In this bit of code, ``vessel.orbit`` returns an :class:`Orbit` object that contains all the
information about the orbit of the rocket.

Part Four: Returning Safely to Kerbin
-------------------------------------

Our Kerbals are now heading on a sub-orbital trajectory and are on a collision course with the
surface. All that remains to do is wait until they fall to 1km altitude above the surface, and then
deploy the parachutes. If you like, you can use time acceleration to skip ahead to just before this
happens - the script will continue to work.

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 67-74

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 42-45

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 42-45

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 53-57

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 37-40

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 36-39

The parachutes should have now been deployed. The next bit of code will repeatedly print out the
altitude of the capsule until its speed reaches zero -- which will happen when it lands:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 76-93

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 47-52

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 47-51

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 59-64

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 42-

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 41-44

This bit of code uses the ``vessel.flight()`` function, as before, but this time it is passed a
:class:`ReferenceFrame` parameter. We want to get the vertical speed of the capsule relative to the
surface of Kerbin, so the values returned by the flight object need to be relative to the surface of
Kerbin. We therefore pass ``vessel.orbit.body.reference_frame`` to ``vessel.flight()`` as this
reference frame has its origin at the center of Kerbin and it rotates with the planet. For more
information, check out the tutorial on :ref:`tutorial-reference-frames`.

Your Kerbals should now have safely landed back on the surface.
