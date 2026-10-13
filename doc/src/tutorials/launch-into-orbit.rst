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
         :lines: 1-48

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 1-40

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 1-38

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :lines: 1-53

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
         :lines: 50-85

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 42-79

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 40-74

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :lines: 55-92

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :lines: 30-64

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :lines: 33-62

Next, the program fine tunes the apoapsis using 25% thrust, then waits until the rocket has
left Kerbin's atmosphere. Each wait is an event, which the server checks every physics tick. See
:ref:`tutorial-server-side-functions` for how events work.

The Python and C# clients compile the event's condition from a lambda, and the C++ and Java
clients build it with the :ref:`expression API <server-side-functions-api>`. The C and Lua
programs poll in a loop, as those clients have no events.

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :lines: 87-103

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 81-99

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 76-101

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :lines: 94-122

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :lines: 66-76

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :lines: 64-76

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
         :lines: 105-134

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 101-119

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 103-121

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :lines: 124-142

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :lines: 78-95

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :lines: 78-95

Next, we need to rotate the craft and wait until the circularization burn. We
orientate the ship along the y-axis of the maneuver node's reference frame
(i.e. in the direction of the burn) then time warp to 5 seconds before the burn.

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :lines: 136-149

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 121-131

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 123-133

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :lines: 144-156

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :lines: 97-107

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :lines: 97-107

Finally, the program waits for an event that fires half the burn time before apoapsis, then sets
maximum throttle to start the burn.

It throttles down to 5% about a tenth of a second before the predicted end of the burn. A last
event fires when the remaining delta-v flips around to point retrograde, which marks the node as
executed:

.. tabs::

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :lines: 151-

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :lines: 133-

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :lines: 135-

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :lines: 158-

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :lines: 109-

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :lines: 109-

The rocket should now be in a circular 150km orbit above Kerbin.
