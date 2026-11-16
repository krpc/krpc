.. currentmodule:: SpaceCenter

Launch into Orbit
=================

This tutorial launches the AeroEquus, one of the game's stock craft, into a
150km circular orbit. Load it in the Vehicle Assembly Building and launch it
to the pad before you run the program.

The program is available in a variety of languages:

:download:`Python</scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py>`,
:download:`C#</scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs>`,
:download:`C++</scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp>`,
:download:`C</scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c>`,
:download:`Java</scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java>`,
:download:`Lua</scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua>`,
:download:`JavaScript</scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.js>`

Preparing for Launch
--------------------

The following code connects to the server, gets the active vessel, sets up a
bunch of streams to get flight telemetry then prepares the rocket for launch.

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.py
         :language: python
         :end-at: vessel.control.throttle = 1.0

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cs
         :language: csharp
         :end-at: vessel.Control.Throttle = 1;

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.cpp
         :language: cpp
         :end-at: vessel.control().set_throttle(1);

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.c
         :language: c
         :end-at: krpc_SpaceCenter_Control_set_Throttle(conn, control, 1);

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.java
         :language: java
         :end-at: vessel.getControl().setThrottle(1);

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.lua
         :language: lua
         :end-at: vessel.control.throttle = 1

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.js
         :language: javascript
         :end-at: await control.setThrottle(1);

Ascent
------

The next part of the program launches the rocket on its four solid boosters.
On every pass, the main loop sets the auto-pilot's target pitch, which
gradually turns the rocket towards the horizon.

The loop also watches the fuel in the current stage. When the boosters run
dry, it activates two stages: one drops the boosters, and the next lights the
core's liquid fuel engine. When the core runs dry, it drops the core and
lights the upper stage the same way. The loop exits when the rocket's apoapsis
is close to the target apoapsis.

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
         :start-at: krpc_SpaceCenter_AutoPilot_set_Engaged(conn, auto_pilot, true);
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

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.js
         :language: javascript
         :start-at: // Activate the first stage
         :end-before: // Disable engines when target apoapsis is reached

Leaving the Atmosphere
----------------------

Next, the program fine tunes the apoapsis using 25% thrust, then waits until the rocket has
left Kerbin's atmosphere. Each wait is an event, which the server checks every physics tick. See
:ref:`server-side-functions` for how events work.

The Python, C# and JavaScript clients compile the event's condition from a lambda, and the C++
and Java clients build it with the :ref:`expression API <server-side-functions-api>`. The C and Lua
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

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.js
         :language: javascript
         :start-at: // Disable engines when target apoapsis is reached
         :end-before: // Plan circularization burn (using vis-viva equation)

Planning the Circularization Burn
---------------------------------

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

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.js
         :language: javascript
         :start-at: // Plan circularization burn (using vis-viva equation)
         :end-at: const burnTime = (m0 - m1) / flowRate;

Waiting for the Burn
--------------------

Next, we need to rotate the craft and wait until the circularization burn. We
point the ship along the node's remaining burn vector, in Kerbin's
non-rotating reference frame, then time warp to 5 seconds before the burn.

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

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.js
         :language: javascript
         :start-at: // Orientate ship
         :end-at: await spaceCenter.warpTo(burnUT - leadTime);

Executing the Burn
------------------

Finally, the program waits for an event that fires half the burn time before apoapsis, then
starts the burn. A loop steers along the remaining burn vector, which turns as the vessel moves
around its orbit. It burns at full throttle, then at 5% for the last 5 m/s.

The burn ends when less than 0.1 m/s remains. It also ends if the remaining delta-v starts to
grow, which means the burn has overshot:

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

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/launch-into-orbit/LaunchIntoOrbit.js
         :language: javascript
         :start-at: // Execute burn

The rocket should now be in a circular 150km orbit above Kerbin.
