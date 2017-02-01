.. currentmodule:: SpaceCenter

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

This tutorial uses the two stage rocket pictured below. The craft file for this
rocket can be :download:`downloaded here </crafts/SubOrbitalFlight.craft>`.

This tutorial includes source code examples for the main client languages that
kRPC supports. The entire program, for your chosen language can be downloaded
from here:

:download:`C#</scripts/SubOrbitalFlight.cs>`,
:download:`C++</scripts/SubOrbitalFlight.cpp>`,
:download:`Java</scripts/SubOrbitalFlight.java>`,
:download:`Lua</scripts/SubOrbitalFlight.lua>`,
:download:`Python</scripts/SubOrbitalFlight.py>`

.. image:: /images/tutorials/SubOrbitalFlight.png
   :align: center

Part One: Preparing for Launch
------------------------------

The first thing we need to do is open a connection to the server. We can also
pass a descriptive name for our script that will appear in the server window in
game:

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/SubOrbitalFlight.cs
         :language: csharp
         :lines: 1-9
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 1-9
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/SubOrbitalFlight.java
         :language: java
         :lines: 1-16
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/SubOrbitalFlight.lua
         :language: lua
         :lines: 1-3
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/SubOrbitalFlight.py
         :language: python
         :lines: 1-3
         :linenos:

Next we need to get an object representing the active vessel. It's via this
object that we will send instructions to the rocket:

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/SubOrbitalFlight.cs
         :language: csharp
         :lines: 11
         :lineno-start: 11
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 11
         :lineno-start: 11
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/SubOrbitalFlight.java
         :language: java
         :lines: 18
         :lineno-start: 18
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/SubOrbitalFlight.lua
         :language: lua
         :lines: 5
         :lineno-start: 5
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/SubOrbitalFlight.py
         :language: python
         :lines: 5
         :lineno-start: 5
         :linenos:

We then need to prepare the rocket for launch. The following code sets the
throttle to maximum and instructs the auto-pilot to hold a pitch and heading of
90° (vertically upwards). It then waits for 1 second for these settings to take
effect.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/SubOrbitalFlight.cs
         :language: csharp
         :lines: 13-16
         :lineno-start: 13
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 13-16
         :lineno-start: 13
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/SubOrbitalFlight.java
         :language: java
         :lines: 21-24
         :lineno-start: 21
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/SubOrbitalFlight.lua
         :language: lua
         :lines: 7-10
         :lineno-start: 7
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/SubOrbitalFlight.py
         :language: python
         :lines: 7-10
         :lineno-start: 7
         :linenos:

Part Two: Lift-off!
-------------------

We're now ready to launch by activating the first stage (equivalent to pressing
the space bar):

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/SubOrbitalFlight.cs
         :language: csharp
         :lines: 18-19
         :lineno-start: 18
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 18-19
         :lineno-start: 18
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/SubOrbitalFlight.java
         :language: java
         :lines: 26-27
         :lineno-start: 26
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/SubOrbitalFlight.lua
         :language: lua
         :lines: 12-13
         :lineno-start: 12
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/SubOrbitalFlight.py
         :language: python
         :lines: 12-13
         :lineno-start: 12
         :linenos:

The rocket has a solid fuel stage that will quickly run out, and will need to be
jettisoned. We can monitor the amount of solid fuel in the rocket using a while
loop that repeatedly checks how much solid fuel there is left in the
rocket. When the loop exits, we will activate the next stage to jettison the
boosters:

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/SubOrbitalFlight.cs
         :language: csharp
         :lines: 21-24
         :lineno-start: 21
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 21-24
         :lineno-start: 21
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/SubOrbitalFlight.java
         :language: java
         :lines: 29-32
         :lineno-start: 29
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/SubOrbitalFlight.lua
         :language: lua
         :lines: 15-19
         :lineno-start: 15
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/SubOrbitalFlight.py
         :language: python
         :lines: 15-18
         :lineno-start: 15
         :linenos:

In this bit of code, ``vessel.resources`` returns a :class:`Resources` object
that is used to get information about the resources in the rocket.

Part Three: Reaching Apoapsis
-----------------------------

Next we will execute a gravity turn when the rocket reaches a sufficiently high
altitude. The following loop repeatedly checks the altitude and exits when the
rocket reaches 10km:

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/SubOrbitalFlight.cs
         :language: csharp
         :lines: 26-27
         :lineno-start: 26
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 26-27
         :lineno-start: 26
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/SubOrbitalFlight.java
         :language: java
         :lines: 34-35
         :lineno-start: 34
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/SubOrbitalFlight.lua
         :language: lua
         :lines: 21-23
         :lineno-start: 21
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/SubOrbitalFlight.py
         :language: python
         :lines: 20-21
         :lineno-start: 20
         :linenos:

In this bit of code, calling ``vessel.flight()`` returns a :class:`Flight`
object that is used to get all sorts of information about the rocket, such as
the direction it is pointing in and its velocity.

Now we need to angle the rocket over to a pitch of 60° and maintain a heading of
90° (west). To do this, we simply reconfigure the auto-pilot:

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/SubOrbitalFlight.cs
         :language: csharp
         :lines: 29-30
         :lineno-start: 29
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 29-30
         :lineno-start: 29
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/SubOrbitalFlight.java
         :language: java
         :lines: 37-38
         :lineno-start: 38
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/SubOrbitalFlight.lua
         :language: lua
         :lines: 25-26
         :lineno-start: 25
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/SubOrbitalFlight.py
         :language: python
         :lines: 23-24
         :lineno-start: 23
         :linenos:

Now we wait until the apoapsis reaches 100km, then reduce the throttle to zero,
jettison the launch stage and turn off the auto-pilot:

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/SubOrbitalFlight.cs
         :language: csharp
         :lines: 32-38
         :lineno-start: 32
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 32-38
         :lineno-start: 32
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/SubOrbitalFlight.java
         :language: java
         :lines: 40-46
         :lineno-start: 40
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/SubOrbitalFlight.lua
         :language: lua
         :lines: 28-35
         :lineno-start: 28
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/SubOrbitalFlight.py
         :language: python
         :lines: 26-32
         :lineno-start: 26
         :linenos:

In this bit of code, ``vessel.orbit`` returns an :class:`Orbit` object that
contains all the information about the orbit of the rocket.

Part Four: Returning Safely to Kerbin
-------------------------------------

Our Kerbals are now heading on a sub-orbital trajectory and are on a collision
course with the surface. All that remains to do is wait until they fall to 1km
altitude above the surface, and then deploy the parachutes. If you like, you can
use time acceleration to skip ahead to just before this happens - the script
will continue to work.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/SubOrbitalFlight.cs
         :language: csharp
         :lines: 40-42
         :lineno-start: 40
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 40-42
         :lineno-start: 40
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/SubOrbitalFlight.java
         :language: java
         :lines: 48-50
         :lineno-start: 48
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/SubOrbitalFlight.lua
         :language: lua
         :lines: 37-40
         :lineno-start: 37
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/SubOrbitalFlight.py
         :language: python
         :lines: 34-36
         :lineno-start: 34
         :linenos:

The parachutes should have now been deployed. The next bit of code will
repeatedly print out the altitude of the capsule until its speed reaches zero --
which will happen when it lands:

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/SubOrbitalFlight.cs
         :language: csharp
         :lines: 44-
         :lineno-start: 44
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/SubOrbitalFlight.cpp
         :language: cpp
         :lines: 44-
         :lineno-start: 44
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/SubOrbitalFlight.java
         :language: java
         :lines: 52-
         :lineno-start: 52
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/SubOrbitalFlight.lua
         :language: lua
         :lines: 42-
         :lineno-start: 42
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/SubOrbitalFlight.py
         :language: python
         :lines: 38-
         :lineno-start: 38
         :linenos:

This bit of code uses the ``vessel.flight()`` function, as before, but this time
it is passed a :class:`ReferenceFrame` parameter. We want to get the vertical
speed of the capsule relative to the surface of Kerbin, so the values returned
by the flight object need to be relative to the surface of Kerbin. We therefore
pass ``vessel.orbit.body.reference_frame`` to ``vessel.flight()`` as this
reference frame has its origin at the center of Kerbin and it rotates with the
planet. For more information, check out the tutorial on
:ref:`tutorial-reference-frames`.

Your Kerbals should now have safely landed back on the surface.
