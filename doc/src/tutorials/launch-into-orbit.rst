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

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :end-at: print("Launch!")

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :end-at: Console.WriteLine ("Launch!");

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :end-at: std::cout << "Launch!" << std::endl;

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :end-at: printf("Launch!\n");

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :end-at: System.out.println("Launch!");

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :end-at: print('Launch!')

The next part of the program launches the rocket. The main loop continuously
updates the auto-pilot heading to gradually pitch the rocket towards the
horizon. It also monitors the amount of solid fuel remaining in the boosters,
separating them when they run dry. The loop exits when the rockets apoapsis is
close to the target apoapsis.

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :start-at: # Activate the first stage
         :end-before: # Disable engines when target apoapsis is reached

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :start-at: // Activate the first stage
         :end-before: // Disable engines when target apoapsis is reached

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :start-at: // Activate the first stage
         :end-before: // Disable engines when target apoapsis is reached

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :start-at: krpc_SpaceCenter_Control_ActivateNextStage(conn, NULL, control);
         :end-before: krpc_SpaceCenter_Control_set_Throttle(conn, control, 0.25);

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :start-at: // Activate the first stage
         :end-before: // Disable engines when target apoapsis is reached

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :start-at: -- Activate the first stage
         :end-before: -- Disable engines when target apoapsis is reached

Next, the program fine tunes the apoapsis using 25% thrust, then waits until the rocket has
left Kerbin's atmosphere. Each wait is an event, which the server checks every physics tick. See
:ref:`server-side-functions` for how events work.

The Python and C# clients compile the event's condition from a lambda, and the C++ and Java
clients build it with the :ref:`expression API <server-side-functions-api>`. The C and Lua
programs poll in a loop, as those clients have no events.

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :start-at: # Disable engines when target apoapsis is reached
         :end-before: # Plan circularization burn (using vis-viva equation)

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :start-at: // Disable engines when target apoapsis is reached
         :end-before: // Plan circularization burn (using vis-viva equation)

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :start-at: // Disable engines when target apoapsis is reached
         :end-before: // Plan circularization burn (using vis-viva equation)

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :start-at: krpc_SpaceCenter_Control_set_Throttle(conn, control, 0.25);
         :end-before: printf("Planning circularization burn\n");

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :start-at: // Disable engines when target apoapsis is reached
         :end-before: // Plan circularization burn (using vis-viva equation)

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :start-at: -- Disable engines when target apoapsis is reached
         :end-before: ---- Plan circularization burn (using vis-viva equation)

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

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :start-at: # Plan circularization burn (using vis-viva equation)
         :end-at: burn_time = (m0 - m1) / flow_rate

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :start-at: // Plan circularization burn (using vis-viva equation)
         :end-at: double burnTime = (m0 - m1) / flowRate;

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :start-at: // Plan circularization burn (using vis-viva equation)
         :end-at: double burn_time = (m0 - m1) / flow_rate;

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :start-at: printf("Planning circularization burn\n");
         :end-at: double burn_time = (m0 - m1) / flow_rate;

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :start-at: // Plan circularization burn (using vis-viva equation)
         :end-at: double burnTime = (m0 - m1) / flowRate;

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :start-at: ---- Plan circularization burn (using vis-viva equation)
         :end-at: local burn_time = (m0 - m1) / flow_rate

Next, we need to rotate the craft and wait until the circularization burn. We
orientate the ship along the y-axis of the maneuver node's reference frame
(i.e. in the direction of the burn) then time warp to 5 seconds before the burn.

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :start-at: # Orientate ship
         :end-at: conn.space_center.warp_to(burn_ut - lead_time)

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :start-at: // Orientate ship
         :end-at: conn.SpaceCenter ().WarpTo (burnUT - leadTime);

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :start-at: // Orientate ship
         :end-at: space_center.warp_to(burn_ut - lead_time);

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :start-at: printf("Orientating ship for circularization burn\n");
         :end-at: krpc_SpaceCenter_WarpTo(conn, burn_ut - lead_time, 100000, 2);

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :start-at: // Orientate ship
         :end-at: spaceCenter.warpTo(burnUt - leadTime, 100000, 2);

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :start-at: -- Orientate ship
         :end-at: conn.space_center.warp_to(burn_ut - lead_time)

Finally, the program waits for an event that fires half the burn time before apoapsis, then sets
maximum throttle to start the burn.

It throttles down to 5% about a tenth of a second before the predicted end of the burn. A last
event fires when the remaining delta-v flips around to point retrograde, which marks the node as
executed:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :start-at: # Execute burn

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :start-at: // Execute burn

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :start-at: // Execute burn

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :start-at: printf("Ready to execute burn\n");

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :start-at: // Execute burn

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :start-at: -- Execute burn

The rocket should now be in a circular 150km orbit above Kerbin.
