.. currentmodule:: SpaceCenter

Launch into Orbit
=================

This tutorial launches a two-stage rocket into a 150km circular orbit. The
program assumes you are using :download:`this craft file
</crafts/LaunchIntoOrbit.craft>`.

The program is available in a variety of languages:

:download:`C</scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c>`,
:download:`C#</scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs>`,
:download:`C++</scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp>`,
:download:`Java</scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java>`,
:download:`Lua</scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua>`,
:download:`Python</scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py>`

The following code connects to the server, gets the active vessel, sets up a
bunch of streams to get flight telemetry then prepares the rocket for launch.

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :lines: 1-42

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 1-40

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 1-36

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :lines: 1-51

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :lines: 1-28

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :lines: 1-31

The next part of the program launches the rocket. The main loop continuously
updates the auto-pilot heading to gradually pitch the rocket towards the
horizon. It also monitors the amount of solid fuel remaining in the boosters,
separating them when they run dry. The loop exits when the rockets apoapsis is
close to the target apoapsis.

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :lines: 44-79

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 42-79

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 38-72

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :lines: 53-90

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :lines: 30-64

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :lines: 33-63

Next, the program fine tunes the apoapsis, using 25% thrust, then waits until
the rocket has left Kerbin's atmosphere.

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :lines: 81-97

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 81-91

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 74-84

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :lines: 92-102

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :lines: 66-76

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :lines: 65-75

It is now time to plan the circularization burn. First, we calculate the delta-v
required to circularize the orbit using the `vis-viva equation
<https://en.wikipedia.org/wiki/Vis-viva_equation>`_. We then calculate the burn
time needed to achieve this delta-v, using the `Tsiolkovsky rocket equation
<https://en.wikipedia.org/wiki/Tsiolkovsky_rocket_equation>`_.

.. note:: The per-stage values that feed this calculation are also available
   directly from the staging API: :meth:`Vessel.stage_at` returns a
   :class:`Stage` object with properties such as :attr:`Stage.delta_v`,
   :attr:`Stage.specific_impulse` and :attr:`Stage.burn_time`. The derivation is
   shown here because we need the burn time for a specific delta-v, rather than
   for burning the whole stage.

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :lines: 99-128

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 93-111

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 86-104

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :lines: 104-122

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :lines: 78-95

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :lines: 77-94

Next, we need to rotate the craft and wait until the circularization burn. We
orientate the ship along the y-axis of the maneuver node's reference frame
(i.e. in the direction of the burn) then time warp to 5 seconds before the burn.

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :lines: 130-143

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 113-123

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 106-116

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :lines: 124-136

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :lines: 97-107

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :lines: 96-106

This next part executes the burn. It sets maximum throttle, then throttles down
to 5% approximately a tenth of a second before the predicted end of the burn. It then
monitors the remaining delta-v until it flips around to point retrograde (at
which point the node has been executed).

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :lines: 145-

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 125-

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 118-

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :lines: 138-

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :lines: 109-

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :lines: 108-

The rocket should now be in a circular 150km orbit above Kerbin.
