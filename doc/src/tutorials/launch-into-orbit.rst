.. currentmodule:: SpaceCenter

Launch into Orbit
=================

This tutorial launches a two-stage rocket into a 150km circular orbit. The
program assumes you are using :download:`this craft file
</crafts/LaunchIntoOrbit.craft>`.

The program is available in a variety of languages:
:download:`C#</scripts/LaunchIntoOrbit.cs>`,
:download:`C++</scripts/LaunchIntoOrbit.cpp>`,
:download:`Python</scripts/LaunchIntoOrbit.py>`,
:download:`Lua</scripts/LaunchIntoOrbit.lua>`,
:download:`Java</scripts/LaunchIntoOrbit.java>`.

The following code connects to the server, gets the active vessel, sets up a
bunch of streams to get flight telemetry then prepares the rocket for launch.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 1-38
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 1-35
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/LaunchIntoOrbit.lua
         :language: lua
         :lines: 1-30
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/LaunchIntoOrbit.java
         :language: java
         :lines: 1-47
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/LaunchIntoOrbit.py
         :language: python
         :lines: 1-31
         :linenos:

The next part of the program launches the rocket. The main loop continuously
updates the auto-pilot heading to gradually pitch the rocket towards the
horizon. It also monitors the amount of solid fuel remaining in the boosters,
separating them when they run dry. The loop exits when the rockets apoapsis is
close to the target apoapsis.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 40-74
         :lineno-start: 40
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 35-70
         :lineno-start: 35
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/LaunchIntoOrbit.lua
         :language: lua
         :lines: 31-59
         :lineno-start: 31
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/LaunchIntoOrbit.java
         :language: java
         :lines: 49-83
         :lineno-start: 49
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/LaunchIntoOrbit.py
         :language: python
         :lines: 33-61
         :lineno-start: 33
         :linenos:

Next, the program fine tunes the apoapsis, using 10% thrust, then waits until
the rocket has left Kerbin's atmosphere.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 76-86
         :lineno-start: 76
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 72-82
         :lineno-start: 72
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/LaunchIntoOrbit.lua
         :language: lua
         :lines: 61-71
         :lineno-start: 61
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/LaunchIntoOrbit.java
         :language: java
         :lines: 85-95
         :lineno-start: 85
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/LaunchIntoOrbit.py
         :language: python
         :lines: 63-73
         :lineno-start: 63
         :linenos:

It is now time to plan the circularization burn. First, we calculate the delta-v
required to circularize the orbit using the `vis-viva equation
<https://en.wikipedia.org/wiki/Vis-viva_equation>`_. We then calculate the burn
time needed to achieve this delta-v, using the `Tsiolkovsky rocket equation
<https://en.wikipedia.org/wiki/Tsiolkovsky_rocket_equation>`_.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 88-105
         :lineno-start: 88
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 84-101
         :lineno-start: 84
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/LaunchIntoOrbit.lua
         :language: lua
         :lines: 73-90
         :lineno-start: 73
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/LaunchIntoOrbit.java
         :language: java
         :lines: 97-114
         :lineno-start: 97
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/LaunchIntoOrbit.py
         :language: python
         :lines: 75-92
         :lineno-start: 75
         :linenos:

Next, we need to rotate the craft and wait until the circularization burn. We
orientate the ship along the y-axis of the maneuver node's reference frame
(i.e. in the direction of the burn) then time warp to 5 seconds before the burn.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 107-117
         :lineno-start: 107
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 103-113
         :lineno-start: 103
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/LaunchIntoOrbit.lua
         :language: lua
         :lines: 92-102
         :lineno-start: 92
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/LaunchIntoOrbit.java
         :language: java
         :lines: 116-126
         :lineno-start: 116
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/LaunchIntoOrbit.py
         :language: python
         :lines: 94-104
         :lineno-start: 94
         :linenos:

This next part executes the burn. It sets maximum throttle, then throttles down
to 5% approximately a tenth of a second before the predicted end of the burn. It then
monitors the remaining delta-v until it flips around to point retrograde (at
which point the node has been executed).

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 119-
         :lineno-start: 119
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 115-
         :lineno-start: 115
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/LaunchIntoOrbit.lua
         :language: lua
         :lines: 104-
         :lineno-start: 104
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/LaunchIntoOrbit.java
         :language: java
         :lines: 128-
         :lineno-start: 128
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/LaunchIntoOrbit.py
         :language: python
         :lines: 106-
         :lineno-start: 106
         :linenos:

The rocket should now be in a circular 150km orbit above Kerbin.
