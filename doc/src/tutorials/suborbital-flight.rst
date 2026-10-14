.. currentmodule:: SpaceCenter

Sub-Orbital Flight
==================

This introductory tutorial uses kRPC to send some Kerbals on a sub-orbital flight, and return
them safely to Kerbin. It covers the following topics:

* Controlling a rocket, by activating stages and setting the throttle
* Using the autopilot to point the vessel in a direction
* Using events to wait for things to happen in the game
* Tracking the amount of resources in the vessel
* Tracking flight and orbital data, such as altitude and apoapsis altitude

.. note::

   For details on how to write scripts and connect to kRPC, see the :ref:`getting-started`
   guide.

The tutorial uses the two stage rocket pictured below. :download:`Download the craft file
</crafts/SubOrbitalFlight.craft>` to fly it yourself.

The tutorial includes source code for the main client languages kRPC supports. Download the
whole program in your chosen language:

:download:`C</scripts/tutorials/suborbital-flight/SubOrbitalFlight.c>`,
:download:`C#</scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs>`,
:download:`C++</scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp>`,
:download:`Java</scripts/tutorials/suborbital-flight/SubOrbitalFlight.java>`,
:download:`Lua</scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua>`,
:download:`Python</scripts/tutorials/suborbital-flight/SubOrbitalFlight.py>`

.. image:: /images/tutorials/SubOrbitalFlight.png
   :align: center

Part One: Preparing for Launch
------------------------------

First we open a connection to the server, passing a name for the script. The name appears in the
server window in the game:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 13-15

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 9

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 10-11

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 15-16

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 1-3

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 4

Next we get an object representing the active vessel. We send instructions to the rocket through
this object:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 17-24

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 11

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 13

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 18

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 5

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 6

Then we prepare the rocket for launch. This sets the throttle to maximum, and has the autopilot
hold a pitch and heading of 90°, pointing straight up. It then waits a second for the settings to
take effect:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 26-29

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 13-16

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 15-18

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 20-23

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 7-10

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 8-11

Part Two: Lift-off!
-------------------

We launch by activating the first stage, which is the same as pressing the space bar:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 31-32

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 18-19

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 20-21

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 25-26

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 12-13

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 13-14

The solid fuel boosters soon run out, and the rocket has to jettison them. We wait for an event
that fires when less than 0.1 units of solid fuel are left, then activate the next stage:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 34-44

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 21-28

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 23-35

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 28-40

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 15-19

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 16-20

``vessel.resources`` returns a :class:`Resources` object, which gives the amounts of the
resources in the rocket. The event is a :ref:`server side function <tutorial-server-side-functions>`
that compares the amount of solid fuel with 0.1. The server checks it every physics tick, and
the script sleeps until it fires.

The Python and C# clients compile the lambda into the function. The C++ and Java clients build it
from expression objects: an embedded call to ``amount``, a constant and a comparison. The C and
Lua clients do not support events, so their scripts poll the amount in a loop.

Part Three: Reaching Apoapsis
-----------------------------

Next we start a gravity turn once the rocket is high enough. This waits for an event that fires
at an altitude of 10 km:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 46-53

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 30-36

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 37-45

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 42-53

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 21-23

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 22-25

``vessel.flight()`` returns a :class:`Flight` object, which gives information about the rocket in
flight, such as its altitude, direction and velocity.

Now we tilt the rocket over to a pitch of 60°, and hold a heading of 90°, due east. The autopilot
takes the new target:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 55-56

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 38-39

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 47-48

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 55-56

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 25-26

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 27-28

Next we wait for an event that fires when the apoapsis reaches 100 km. Then we cut the throttle,
jettison the launch stage and turn off the autopilot:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 58-71

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 41-52

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 50-64

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 58-75

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 28-35

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 30-38

``vessel.orbit`` returns an :class:`Orbit` object, which describes the orbit of the rocket.

Part Four: Returning Safely to Kerbin
-------------------------------------

The Kerbals are now on a sub-orbital trajectory, on a collision course with the surface. We wait
until they fall to 1 km above the surface, then deploy the parachutes. You can use time warp to
skip ahead to just before this happens, and the script carries on working:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 73-80

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 54-61

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 66-75

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 77-89

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 37-40

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 40-44

The parachutes are now deployed. This prints the altitude of the capsule once a second, until its
vertical speed reaches zero when it lands:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.c
         :language: c
         :lines: 82-99

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cs
         :language: csharp
         :lines: 63-67

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 77-81

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.java
         :language: java
         :lines: 91-95

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.lua
         :language: lua
         :lines: 42-

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/suborbital-flight/SubOrbitalFlight.py
         :language: python
         :lines: 46-49

This calls ``vessel.flight()`` again, now with a :class:`ReferenceFrame`. We want the vertical
speed relative to the surface of Kerbin, so the flight object has to give values in a frame fixed
to Kerbin. ``vessel.orbit.body.reference_frame`` has its origin at the center of Kerbin, and
rotates with the planet. See the :ref:`tutorial-reference-frames` tutorial for more.

Your Kerbals should now be safely back on the surface.
