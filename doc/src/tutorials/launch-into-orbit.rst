Launch into Orbit
=================

This tutorial launches a two-stage rocket into a 150km circular orbit. The craft
file for the rocket can be :download:`downloaded here
</crafts/LaunchIntoOrbit.craft>` and the entire python script :download:`from
here </scripts/LaunchIntoOrbit.py>`.

The following code connects to the server, gets the active vessel, sets up a
bunch streams to get flight telemetry then prepares the rocket for launch.

.. literalinclude:: /scripts/LaunchIntoOrbit.py
   :lines: 1-30

The next part of the program launches the rocket. The main loop continuously
updates the auto-pilot heading to gradually pitch the rocket towards the
horizon. It also monitors the amount of solid fuel remaining in the boosters,
separating them when they run dry. The loop exits when the rockets apoapsis is
close to the target apoapsis.

.. literalinclude:: /scripts/LaunchIntoOrbit.py
   :lines: 32-60

Next, the program fine tunes the apoapsis, using 10% thrust, then waits until
the rocket has left Kerbin's atmosphere.

.. literalinclude:: /scripts/LaunchIntoOrbit.py
   :lines: 62-72

It is now time to plan the circularization burn. First, we calculate the delta-v
required to circularize the orbit using the `vis-viva equation
<http://en.wikipedia.org/wiki/Vis-viva_equation>`_. We then calculate the burn
time needed to achieve this delta-v, using the `Tsiolkovsky rocket equation
<http://en.wikipedia.org/wiki/Tsiolkovsky_rocket_equation>`_.

.. literalinclude:: /scripts/LaunchIntoOrbit.py
   :lines: 74-91

Next, we need to rotate the craft and wait until the circularization burn. We
orientate the ship along the y-axis of the maneuver node's reference frame
(i.e. in the direction of the burn) then time warp to 5 seconds before the burn.

.. literalinclude:: /scripts/LaunchIntoOrbit.py
   :lines: 93-103

This next part executes the burn. It sets maximum throttle, then throttles down
to 5% approximately a tenth of a second before the predicted end of the burn. It then
monitors the remaining delta-v until it flips around to point retrograde (at
which point the node has been executed).

.. literalinclude:: /scripts/LaunchIntoOrbit.py
   :lines: 105-

The rocket should now be in a circular 150km orbit above Kerbin.
