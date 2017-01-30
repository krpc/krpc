.. currentmodule:: SpaceCenter

Launch into Orbit
=================

This tutorial launches a two-stage rocket into a 150km circular orbit. The
program assumes you are using :download:`this craft file
</crafts/LaunchIntoOrbit.craft>`.

The program is available in a variety of languages:

:download:`C#</scripts/LaunchIntoOrbit.cs>`,
:download:`C++</scripts/LaunchIntoOrbit.cpp>`,
:download:`Java</scripts/LaunchIntoOrbit.java>`,
:download:`Lua</scripts/LaunchIntoOrbit.lua>`,
:download:`Python</scripts/LaunchIntoOrbit.py>`

The following code connects to the server, gets the active vessel, sets up a
bunch of streams to get flight telemetry then prepares the rocket for launch.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 1-39
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 1-35
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/LaunchIntoOrbit.java
         :language: java
         :lines: 1-50
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/LaunchIntoOrbit.lua
         :language: lua
         :lines: 1-30
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
         :lines: 41-78
         :lineno-start: 41
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 37-71
         :lineno-start: 37
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/LaunchIntoOrbit.java
         :language: java
         :lines: 52-89
         :lineno-start: 52
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/LaunchIntoOrbit.lua
         :language: lua
         :lines: 31-65
         :lineno-start: 31
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/LaunchIntoOrbit.py
         :language: python
         :lines: 33-62
         :lineno-start: 33
         :linenos:

Next, the program fine tunes the apoapsis, using 10% thrust, then waits until
the rocket has left Kerbin's atmosphere.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 80-90
         :lineno-start: 80
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 73-83
         :lineno-start: 73
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/LaunchIntoOrbit.java
         :language: java
         :lines: 91-101
         :lineno-start: 91
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/LaunchIntoOrbit.lua
         :language: lua
         :lines: 67-77
         :lineno-start: 67
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/LaunchIntoOrbit.py
         :language: python
         :lines: 64-74
         :lineno-start: 64
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
         :lines: 92-110
         :lineno-start: 92
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 85-103
         :lineno-start: 85
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/LaunchIntoOrbit.java
         :language: java
         :lines: 103-121
         :lineno-start: 103
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/LaunchIntoOrbit.lua
         :language: lua
         :lines: 79-96
         :lineno-start: 79
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/LaunchIntoOrbit.py
         :language: python
         :lines: 76-94
         :lineno-start: 76
         :linenos:

Next, we need to rotate the craft and wait until the circularization burn. We
orientate the ship along the y-axis of the maneuver node's reference frame
(i.e. in the direction of the burn) then time warp to 5 seconds before the burn.

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 112-122
         :lineno-start: 112
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 105-115
         :lineno-start: 105
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/LaunchIntoOrbit.java
         :language: java
         :lines: 123-135
         :lineno-start: 123
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/LaunchIntoOrbit.lua
         :language: lua
         :lines: 98-108
         :lineno-start: 98
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/LaunchIntoOrbit.py
         :language: python
         :lines: 96-106
         :lineno-start: 96
         :linenos:

This next part executes the burn. It sets maximum throttle, then throttles down
to 5% approximately a tenth of a second before the predicted end of the burn. It then
monitors the remaining delta-v until it flips around to point retrograde (at
which point the node has been executed).

.. tabs::

   .. tab:: C#

      .. literalinclude:: /scripts/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 124-
         :lineno-start: 124
         :linenos:

   .. tab:: C++

      .. literalinclude:: /scripts/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 117-
         :lineno-start: 115
         :linenos:

   .. tab:: Java

      .. literalinclude:: /scripts/LaunchIntoOrbit.java
         :language: java
         :lines: 137-
         :lineno-start: 137
         :linenos:

   .. tab:: Lua

      .. literalinclude:: /scripts/LaunchIntoOrbit.lua
         :language: lua
         :lines: 110-
         :lineno-start: 110
         :linenos:

   .. tab:: Python

      .. literalinclude:: /scripts/LaunchIntoOrbit.py
         :language: python
         :lines: 108-
         :lineno-start: 108
         :linenos:

The rocket should now be in a circular 150km orbit above Kerbin.
