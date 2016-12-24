.. currentmodule:: SpaceCenter

Launch into Orbit
=================

This tutorial launches a two-stage rocket into a 150km circular orbit. The
program assumes you are using :download:`this craft file
</crafts/LaunchIntoOrbit.craft>`. The program is available in a variety of
languages:

* :download:`C# </scripts/LaunchIntoOrbit.cs>`
* :download:`C++ </scripts/LaunchIntoOrbit.cpp>`
* :download:`Python </scripts/LaunchIntoOrbit.py>`
* :download:`Lua </scripts/LaunchIntoOrbit.lua>`
* :download:`Java </scripts/LaunchIntoOrbit.java>`

The following code connects to the server, gets the active vessel, sets up a
bunch of streams to get flight telemetry then prepares the rocket for launch.

.. literalinclude:: /scripts/LaunchIntoOrbit.py
   :lines: 1-31
   :linenos:

The next part of the program launches the rocket. The main loop continuously
updates the auto-pilot heading to gradually pitch the rocket towards the
horizon. It also monitors the amount of solid fuel remaining in the boosters,
separating them when they run dry. The loop exits when the rockets apoapsis is
close to the target apoapsis.

.. literalinclude:: /scripts/LaunchIntoOrbit.py
   :lines: 33-61

Next, the program fine tunes the apoapsis, using 10% thrust, then waits until
the rocket has left Kerbin's atmosphere.

.. literalinclude:: /scripts/LaunchIntoOrbit.py
   :lines: 63-73

It is now time to plan the circularization burn. First, we calculate the delta-v
required to circularize the orbit using the `vis-viva equation
<https://en.wikipedia.org/wiki/Vis-viva_equation>`_. We then calculate the burn
time needed to achieve this delta-v, using the `Tsiolkovsky rocket equation
<https://en.wikipedia.org/wiki/Tsiolkovsky_rocket_equation>`_.

.. literalinclude:: /scripts/LaunchIntoOrbit.py
   :lines: 75-92

Next, we need to rotate the craft and wait until the circularization burn. We
orientate the ship along the y-axis of the maneuver node's reference frame
(i.e. in the direction of the burn) then time warp to 5 seconds before the burn.

.. literalinclude:: /scripts/LaunchIntoOrbit.py
   :lines: 94-104

This next part executes the burn. It sets maximum throttle, then throttles down
to 5% approximately a tenth of a second before the predicted end of the burn. It then
monitors the remaining delta-v until it flips around to point retrograde (at
which point the node has been executed).

.. literalinclude:: /scripts/LaunchIntoOrbit.py
   :lines: 106-

The rocket should now be in a circular 150km orbit above Kerbin.
