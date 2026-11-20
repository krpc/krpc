.. currentmodule:: MechJeb

MechJeb
=======

`MechJeb <https://github.com/MuMech/MechJeb2>`_ is an autopilot. The :doc:`MechJeb service
</python/api/mechjeb>` follows MechJeb's windows: a script sets what a player would set in a
window, then engages the autopilot. This tutorial walks through a script that flies the stock
PT Series Munsplorer from the launch pad to the surface of Minmus. MechJeb adds itself to every
command pod, so the craft needs no extra part.

The tutorial includes the script in each of the client languages kRPC supports. Download the
whole program in your chosen language:

:download:`Python</scripts/tutorials/mechjeb/LaunchToMinmus.py>`,
:download:`C#</scripts/tutorials/mechjeb/LaunchToMinmus.cs>`,
:download:`C++</scripts/tutorials/mechjeb/LaunchToMinmus.cpp>`,
:download:`C</scripts/tutorials/mechjeb/LaunchToMinmus.c>`,
:download:`Java</scripts/tutorials/mechjeb/LaunchToMinmus.java>`,
:download:`Lua</scripts/tutorials/mechjeb/LaunchToMinmus.lua>`,
:download:`JavaScript</scripts/tutorials/mechjeb/LaunchToMinmus.js>`

The script starts with its helpers. ``wait_for`` waits until a condition is true, with an event.
``print_steps`` prints each step of an autopilot, from a stream of its status. ``print_orbit``
prints the vessel's orbit. The C and Lua clients have no events or streams, so their scripts
poll. Each waits for an autopilot in a loop that prints its steps, and the C program defines
``sleep_ms`` to pause between calls:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.py
         :language: python
         :end-before: # The target orbit, set as in

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cs
         :language: csharp
         :start-at: var conn = new Connection ("Launch to Minmus");
         :end-before: // The target orbit, set as in

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cpp
         :language: cpp
         :end-before: // The target orbit, set as in

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.c
         :language: c
         :end-before: // The target orbit, set as in

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.java
         :language: java
         :start-at: public class LaunchToMinmus {
         :end-before: // The target orbit, set as in

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.lua
         :language: lua
         :end-before: -- The target orbit, set as in

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.js
         :language: javascript
         :end-before: // The target orbit, set as in

Ascent to Orbit
---------------

The first part of the script launches the vessel into a 100 km circular orbit with Ascent
Guidance, and prints each step of the autopilot as it climbs:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.py
         :language: python
         :start-at: # The target orbit, set as in
         :end-at: print_orbit()

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cs
         :language: csharp
         :start-at: // The target orbit, set as in
         :end-at: PrintOrbit ();

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cpp
         :language: cpp
         :start-at: // The target orbit, set as in
         :end-at: print_orbit(vessel);

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.c
         :language: c
         :start-at: // The target orbit, set as in
         :end-at: print_orbit(conn, vessel);

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.java
         :language: java
         :start-at: // The target orbit, set as in
         :end-at: printOrbit();

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.lua
         :language: lua
         :start-at: -- The target orbit, set as in
         :end-at: print_orbit()

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.js
         :language: javascript
         :start-at: // The target orbit, set as in
         :end-at: await printOrbit();

The script sets the target orbit on :class:`AscentGuidance`, and the settings of MechJeb's
Ascent Settings window on :attr:`AscentGuidance.settings`. With :attr:`AscentSettings.autostage`
on, MechJeb stages the vessel once the first stage is activated. The autopilot disengages once
it has circularized the orbit.

:attr:`AscentGuidance.ascent_type` picks the ascent path. The classic path flies a turn whose
shape :attr:`AscentGuidance.classic_path` sets. The PVG path, Primer Vector Guidance, computes
its own path, and is the one for Realism Overhaul. Its settings are on
:attr:`AscentGuidance.pvg_settings`.

Transfer to Minmus
------------------

The second part takes the vessel from its orbit of Kerbin to a 20 km orbit of Minmus. It plans
each maneuver with the Maneuver Planner, and has MechJeb execute it:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.py
         :language: python
         :start-at: planner = mj.maneuver_planner
         :end-at: print_orbit()

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cs
         :language: csharp
         :start-at: var planner = mj.ManeuverPlanner;
         :end-at: PrintOrbit ();

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cpp
         :language: cpp
         :start-at: auto planner = mj.maneuver_planner();
         :end-at: print_orbit(vessel);

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.c
         :language: c
         :start-at: krpc_MechJeb_ManeuverPlanner_t planner;
         :end-at: print_orbit(conn, vessel);

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.java
         :language: java
         :start-at: ManeuverPlanner planner = mj.getManeuverPlanner();
         :end-at: printOrbit();

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.lua
         :language: lua
         :start-at: local planner = mj.maneuver_planner
         :end-at: print_orbit()

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.js
         :language: javascript
         :start-at: const planner = await mj.getManeuverPlanner();
         :end-at: await printOrbit();

Each operation of :class:`ManeuverPlanner` is an object with the settings of the planner's
window. Its ``make_nodes`` method, such as :meth:`OperationPlane.make_nodes`, adds the
maneuver nodes, which the planner then executes:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.py
         :language: python
         :start-at: def execute_nodes():
         :end-at: wait_for(lambda: not planner.executing_node)

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cs
         :language: csharp
         :start-at: // Execute the vessel's maneuver nodes
         :end-before: // Target Minmus

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cpp
         :language: cpp
         :start-at: // Execute the vessel's maneuver nodes
         :end-before: // Target Minmus

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.c
         :language: c
         :start-at: // Execute the vessel's maneuver nodes
         :end-before: int main() {

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.java
         :language: java
         :start-at: /** Execute the vessel's maneuver nodes
         :end-before: static void printOrbit() {

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.lua
         :language: lua
         :start-at: -- Execute the vessel's maneuver nodes
         :end-before: -- Target Minmus

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.js
         :language: javascript
         :start-at: /** Execute the vessel's maneuver nodes
         :end-before: // Target Minmus

:attr:`ManeuverPlanner.autowarp` has MechJeb warp time to each burn. While a node executes,
:attr:`ManeuverPlanner.executing_node` is true.

:attr:`AscentSettings.autostage` applies only during the ascent. For the burns, the script
turns on the autostaging of MechJeb's Utilities window, :attr:`Utilities.staging`, which stages
the vessel once a stage burns out:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.py
         :language: python
         :start-at: # Stage once a stage burns out
         :end-at: mj.utilities.staging.enabled = True

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cs
         :language: csharp
         :start-at: // Stage once a stage burns out
         :end-at: mj.Utilities.Staging.Enabled = true;

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cpp
         :language: cpp
         :start-at: // Stage once a stage burns out
         :end-at: mj.utilities().staging().set_enabled(true);

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.c
         :language: c
         :start-at: // Stage once a stage burns out
         :end-at: krpc_MechJeb_Autostaging_set_Enabled(conn, staging, true);

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.java
         :language: java
         :start-at: // Stage once a stage burns out
         :end-at: mj.getUtilities().getStaging().setEnabled(true);

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.lua
         :language: lua
         :start-at: -- Stage once a stage burns out
         :end-at: mj.utilities.staging.enabled = true

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.js
         :language: javascript
         :start-at: // Stage once a stage burns out
         :end-at: await staging.setEnabled(true);

MechJeb plans the transfer to its own target, which the game's target sets. MechJeb reads the
target on its next update, so the script waits for :attr:`TargetController.normal_target_exists`:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.py
         :language: python
         :start-at: # Target Minmus, and wait
         :end-before: # Match planes with Minmus

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cs
         :language: csharp
         :start-at: // Target Minmus, and wait
         :end-before: // Match planes with Minmus

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cpp
         :language: cpp
         :start-at: // Target Minmus, and wait
         :end-before: // Match planes with Minmus

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.c
         :language: c
         :start-at: // Target Minmus, and wait
         :end-before: // Match planes with Minmus

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.java
         :language: java
         :start-at: // Target Minmus, and wait
         :end-before: // Match planes with Minmus

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.lua
         :language: lua
         :start-at: -- Target Minmus, and wait
         :end-before: -- Match planes with Minmus

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.js
         :language: javascript
         :start-at: // Target Minmus, and wait
         :end-before: // Match planes with Minmus

Minmus orbits Kerbin at an inclination of 6 degrees, and the vessel's orbit is equatorial. The
plane change comes first, at the node where the two orbits cross:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.py
         :language: python
         :start-at: # Match planes with Minmus
         :end-before: # The Hohmann transfer

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cs
         :language: csharp
         :start-at: // Match planes with Minmus
         :end-before: // The Hohmann transfer

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cpp
         :language: cpp
         :start-at: // Match planes with Minmus
         :end-before: // The Hohmann transfer

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.c
         :language: c
         :start-at: // Match planes with Minmus
         :end-before: // The Hohmann transfer

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.java
         :language: java
         :start-at: // Match planes with Minmus
         :end-before: // The Hohmann transfer

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.lua
         :language: lua
         :start-at: -- Match planes with Minmus
         :end-before: -- The Hohmann transfer

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.js
         :language: javascript
         :start-at: // Match planes with Minmus
         :end-before: // The Hohmann transfer

Each operation picks the time of its maneuver with its :class:`TimeSelector`.
:attr:`TimeReference.rel_nearest_ad` is the nearer of the relative ascending and descending
nodes.

The Hohmann transfer burns once to reach Minmus' orbit. Minmus has a sphere of influence, so
the transfer stops at the encounter, with :attr:`OperationTransfer.capture` off:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.py
         :language: python
         :start-at: # The Hohmann transfer
         :end-before: # Fine tune the encounter

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cs
         :language: csharp
         :start-at: // The Hohmann transfer
         :end-before: // Fine tune the encounter

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cpp
         :language: cpp
         :start-at: // The Hohmann transfer
         :end-before: // Fine tune the encounter

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.c
         :language: c
         :start-at: // The Hohmann transfer
         :end-before: // Fine tune the encounter

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.java
         :language: java
         :start-at: // The Hohmann transfer
         :end-before: // Fine tune the encounter

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.lua
         :language: lua
         :start-at: -- The Hohmann transfer
         :end-before: -- Fine tune the encounter

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.js
         :language: javascript
         :start-at: // The Hohmann transfer
         :end-before: // Fine tune the encounter

:attr:`TimeReference.computed` has MechJeb compute the time of the burn, at the phase angle
that reaches Minmus.

The transfer orbit passes Minmus at some distance. A course correction moves the periapsis at
Minmus to 20 km:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.py
         :language: python
         :start-at: # Fine tune the encounter
         :end-before: # Warp into Minmus

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cs
         :language: csharp
         :start-at: // Fine tune the encounter
         :end-before: // Warp into Minmus

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cpp
         :language: cpp
         :start-at: // Fine tune the encounter
         :end-before: // Warp into Minmus

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.c
         :language: c
         :start-at: // Fine tune the encounter
         :end-before: // Warp into Minmus

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.java
         :language: java
         :start-at: // Fine tune the encounter
         :end-before: // Warp into Minmus

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.lua
         :language: lua
         :start-at: -- Fine tune the encounter
         :end-before: -- Warp into Minmus

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.js
         :language: javascript
         :start-at: // Fine tune the encounter
         :end-before: // Warp into Minmus

Last, the script warps into Minmus' sphere of influence, and circularizes the orbit at its
periapsis:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.py
         :language: python
         :start-at: # Warp into Minmus
         :end-before: sc.clear_target()

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cs
         :language: csharp
         :start-at: // Warp into Minmus
         :end-before: sc.ClearTarget ();

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cpp
         :language: cpp
         :start-at: // Warp into Minmus
         :end-before: sc.clear_target();

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.c
         :language: c
         :start-at: // Warp into Minmus
         :end-before: krpc_SpaceCenter_ClearTarget(conn);

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.java
         :language: java
         :start-at: // Warp into Minmus
         :end-before: sc.clearTarget();

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.lua
         :language: lua
         :start-at: -- Warp into Minmus
         :end-before: sc.clear_target()

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.js
         :language: javascript
         :start-at: // Warp into Minmus
         :end-before: await sc.clearTarget();

.. note::

   To launch straight into Minmus' plane, and skip the plane change, target Minmus, enable the
   autopilot, and call :meth:`AscentGuidance.launch_to_target_plane` in place of activating the
   first stage. MechJeb warps to the launch time and stages the vessel itself.

Landing on Minmus
-----------------

The last part lands the vessel on Minmus' Great Flats with Landing Guidance, and prints the
autopilot's steps as it descends:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.py
         :language: python
         :start-at: # Stage until the next stage

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cs
         :language: csharp
         :start-at: // Stage until the next stage

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cpp
         :language: cpp
         :start-at: // Stage until the next stage

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.c
         :language: c
         :start-at: // Stage until the next stage

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.java
         :language: java
         :start-at: // Stage until the next stage

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.lua
         :language: lua
         :start-at: -- Stage until the next stage

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.js
         :language: javascript
         :start-at: // Stage until the next stage

The landing legs are on the lander, above the transfer stage. The legs'
:attr:`SpaceCenter.Part.decouple_stage` is the stage that drops the lander. The script stages
until that stage is next, which drops the transfer stage and activates the lander's engine.
It first turns off MechJeb's autostaging, so the script alone stages the lander:

.. tabs::

   .. group-tab:: Python

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.py
         :language: python
         :start-at: # Stage until the next stage
         :end-before: # Land on Minmus' Great Flats

   .. group-tab:: C#

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cs
         :language: csharp
         :start-at: // Stage until the next stage
         :end-before: // Land on Minmus' Great Flats

   .. group-tab:: C++

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.cpp
         :language: cpp
         :start-at: // Stage until the next stage
         :end-before: // Land on Minmus' Great Flats

   .. group-tab:: C

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.c
         :language: c
         :start-at: // Stage until the next stage
         :end-before: // Land on Minmus' Great Flats

   .. group-tab:: Java

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.java
         :language: java
         :start-at: // Stage until the next stage
         :end-before: // Land on Minmus' Great Flats

   .. group-tab:: Lua

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.lua
         :language: lua
         :start-at: -- Stage until the next stage
         :end-before: -- Land on Minmus' Great Flats

   .. group-tab:: JavaScript

      .. literalinclude:: /scripts/tutorials/mechjeb/LaunchToMinmus.js
         :language: javascript
         :start-at: // Stage until the next stage
         :end-before: // Land on Minmus' Great Flats

MechJeb keeps the throttle at 5% or more. At full thrust, the lander's engine is too strong for
Minmus' weak gravity, and the lander climbs even at 5% throttle. A thrust limit of 30% lets the
lowest throttle leave the lander falling. :attr:`ThrustLimiter.min_throttle` sets that lowest
throttle.

:meth:`LandingGuidance.set_target` sets the landing target, as a latitude and longitude. The
autopilot burns to deorbit over the target, then brakes to a hover 200 m above it. Once it has
stopped drifting, it descends at :attr:`LandingGuidance.touchdown_speed`, and disengages once
the vessel has landed.

.. seealso::

   :doc:`/python/api/mechjeb/migration` for scripts written for KRPC.MechJeb 0.7.1.
