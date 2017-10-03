.. currentmodule:: SpaceCenter

Sub-Orbital Flight
==================

This introductory tutorial uses kRPC to send some Kerbals on a sub-orbital flight, and (hopefully)
returns them safely back to Kerbin. It covers the following topics:

* Controlling a rocket (activating stages, setting the throttle)
* Using the auto pilot to point the vessel in a specific direction
* Using events to wait for things to happen in game
* Tracking the amount of resources in the vessel
* Tracking flight and orbital data (such as altitude and apoapsis altitude)

.. note:: For details on how to write scripts and connect to kRPC, see the :ref:`getting-started`
          guide.

This tutorial uses the two stage rocket pictured below. The craft file for this rocket can be
:download:`downloaded here </crafts/SubOrbitalFlight.craft>`.

This tutorial includes source code examples for the main client languages that kRPC supports. The
entire program, for your chosen language can be downloaded from here:

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

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 10
         :lineno-start: 10
         :linenos:

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 9-11
         :lineno-start: 9
         :linenos:

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 20-22
         :lineno-start: 20
         :linenos:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 1-3
         :linenos:

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 3
         :lineno-start: 3
         :linenos:

Next we need to get an object representing the active vessel. It's via this object that we will send
instructions to the rocket:

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 12
         :lineno-start: 12
         :linenos:

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 13
         :lineno-start: 13
         :linenos:

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 24
         :lineno-start: 24
         :linenos:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 5
         :lineno-start: 5
         :linenos:

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 5
         :lineno-start: 5
         :linenos:

We then need to prepare the rocket for launch. The following code sets the throttle to maximum and
instructs the auto-pilot to hold a pitch and heading of 90° (vertically upwards). It then waits for
1 second for these settings to take effect.

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 14-17
         :lineno-start: 14
         :linenos:

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 15-18
         :lineno-start: 15
         :linenos:

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 26-29
         :lineno-start: 26
         :linenos:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 7-10
         :lineno-start: 7
         :linenos:

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 7-10
         :lineno-start: 7
         :linenos:

Part Two: Lift-off!
-------------------

We're now ready to launch by activating the first stage (equivalent to pressing the space bar):

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 19-20
         :lineno-start: 19
         :linenos:

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 20-21
         :lineno-start: 20
         :linenos:

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 31-32
         :lineno-start: 31
         :linenos:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 12-13
         :lineno-start: 12
         :linenos:

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 12-13
         :lineno-start: 12
         :linenos:

The rocket has a solid fuel stage that will quickly run out, and will need to be jettisoned. We can
monitor the amount of solid fuel in the rocket using an event that is triggered when there is very
little solid fuel left in the rocket. When the event is triggered, we can activate the next stage to
jettison the boosters:

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 23-29
         :lineno-start: 23
         :linenos:

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 26-32
         :lineno-start: 26
         :linenos:

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 34-47
         :lineno-start: 34
         :linenos:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 15-19
         :lineno-start: 15
         :linenos:

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 15-23
         :lineno-start: 15
         :linenos:

In this bit of code, ``vessel.resources`` returns a :class:`Resources` object that is used to get
information about the resources in the rocket. The code creates the expression
``vessel.resources.amount('SolidFuel') < 0.1`` on the server, using the expression API. This
expression is then used to drive an event, which is triggered when the expression returns true.

Part Three: Reaching Apoapsis
-----------------------------

Next we will execute a gravity turn when the rocket reaches a sufficiently high altitude. The
following uses an event to wait until the altitude of the rocket reaches 10km:

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 36-42
         :lineno-start: 36
         :linenos:

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 39-45
         :lineno-start: 39
         :linenos:

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 50-58
         :lineno-start: 50
         :linenos:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 21-23
         :lineno-start: 21
         :linenos:

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 25-31
         :lineno-start: 25
         :linenos:

In this bit of code, calling ``vessel.flight()`` returns a :class:`Flight` object that is used to
get all sorts of information about the rocket, such as the direction it is pointing in and its
velocity.

Now we need to angle the rocket over to a pitch of 60° and maintain a heading of 90° (west). To do
this, we simply reconfigure the auto-pilot:

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 45-46
         :lineno-start: 45
         :linenos:

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 48-49
         :lineno-start: 48
         :linenos:

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 61-62
         :lineno-start: 61
         :linenos:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 25-26
         :lineno-start: 25
         :linenos:

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 33-34
         :lineno-start: 33
         :linenos:

Now we wait until the apoapsis reaches 100km (again, using an event), then reduce the throttle to
zero, jettison the launch stage and turn off the auto-pilot:

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 48-62
         :lineno-start: 32
         :linenos:

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 51-65
         :lineno-start: 51
         :linenos:

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 64-81
         :lineno-start: 64
         :linenos:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 28-35
         :lineno-start: 28
         :linenos:

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 36-48
         :lineno-start: 36
         :linenos:

In this bit of code, ``vessel.orbit`` returns an :class:`Orbit` object that contains all the
information about the orbit of the rocket.

Part Four: Returning Safely to Kerbin
-------------------------------------

Our Kerbals are now heading on a sub-orbital trajectory and are on a collision course with the
surface. All that remains to do is wait until they fall to 1km altitude above the surface, and then
deploy the parachutes. If you like, you can use time acceleration to skip ahead to just before this
happens - the script will continue to work.

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 64-74
         :lineno-start: 64
         :linenos:

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 67-77
         :lineno-start: 67
         :linenos:

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 83-96
         :lineno-start: 83
         :linenos:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 37-40
         :lineno-start: 37
         :linenos:

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 50-58
         :lineno-start: 50
         :linenos:

The parachutes should have now been deployed. The next bit of code will repeatedly print out the
altitude of the capsule until its speed reaches zero -- which will happen when it lands:

.. tabs::

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 76-81
         :lineno-start: 76
         :linenos:

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 79-83
         :lineno-start: 79
         :linenos:

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 98-103
         :lineno-start: 98
         :linenos:

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 42-
         :lineno-start: 42
         :linenos:

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/sub-orbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 60-63
         :lineno-start: 60
         :linenos:

This bit of code uses the ``vessel.flight()`` function, as before, but this time it is passed a
:class:`ReferenceFrame` parameter. We want to get the vertical speed of the capsule relative to the
surface of Kerbin, so the values returned by the flight object need to be relative to the surface of
Kerbin. We therefore pass ``vessel.orbit.body.reference_frame`` to ``vessel.flight()`` as this
reference frame has its origin at the center of Kerbin and it rotates with the planet. For more
information, check out the tutorial on :ref:`tutorial-reference-frames`.

Your Kerbals should now have safely landed back on the surface.
