Launching into Orbit
====================

This tutorial launches the game's Rocket from Cape Canaveral into a circular orbit
200 km up. The script flies the ascent, stages when the first stage runs out of
propellant, and has the flight computer execute the circularization burn.

The complete script is :download:`launch_into_orbit.py </scripts/tutorials/launch-into-orbit/launch_into_orbit.py>`.

The Ascent Profile
------------------

Earth's atmosphere is deep, so the rocket climbs most of the way through it before it
flies level. The gravity turn starts at 1 km and ends at 150 km, and the target apoapsis is
above the top of the atmosphere:

.. literalinclude:: /scripts/tutorials/launch-into-orbit/launch_into_orbit.py
   :language: python
   :start-at: TURN_START_ALTITUDE =
   :end-at: TARGET_ALTITUDE =

Launching
---------

We launch the vehicle on the pad, point it straight up and light the first stage:

.. literalinclude:: /scripts/tutorials/launch-into-orbit/launch_into_orbit.py
   :language: python
   :start-at: vehicle = ksa.launch_vehicle
   :end-at: vehicle = ksa.launch_vehicle

.. literalinclude:: /scripts/tutorials/launch-into-orbit/launch_into_orbit.py
   :language: python
   :start-at: auto_pilot.target_pitch_and_heading(90, 90)
   :end-at: control.engine_on = True

The Gravity Turn
----------------

The loop pitches the rocket over toward the east as it climbs, until the apoapsis reaches
the target. The pitch follows a square root of the altitude, so the rocket turns fastest
low down:

.. literalinclude:: /scripts/tutorials/launch-into-orbit/launch_into_orbit.py
   :language: python
   :start-at: while apoapsis() < TARGET_ALTITUDE:
   :end-at: control.engine_on = False

A stage that has run dry has no thrust available. Staging activates the next two
sequences, the decoupler that drops the spent stage and the engine of the next one:

.. literalinclude:: /scripts/tutorials/launch-into-orbit/launch_into_orbit.py
   :language: python
   :start-at: def stage():
   :end-before: def circularization_burn

Circularizing
-------------

An event waits for the rocket to leave the atmosphere. The client computes the atmosphere's
altitude once, and the server compares the rocket's altitude with it every frame:

.. literalinclude:: /scripts/tutorials/launch-into-orbit/launch_into_orbit.py
   :language: python
   :start-at: atmosphere_altitude =
   :end-at: above_atmosphere.wait()

:doc:`/user-guide/server-side-functions` covers events, and the other server side functions.
Once the rocket is out of the atmosphere, we plan a burn at the apoapsis that makes the
orbit circular, as in :doc:`circularize`:

.. literalinclude:: /scripts/tutorials/launch-into-orbit/launch_into_orbit.py
   :language: python
   :start-at: def circularization_burn(ut):
   :end-at: return vehicle.add_burn

We warp to the burn, and turn on the auto-burn. The flight computer turns it off when the burn
is complete, or when the engines run out of propellant, and an event waits for either. A stage
can run dry during the burn. The game then removes the burn, so we stage and plan the rest of
it. The script stops if no stage is left to fire:

.. literalinclude:: /scripts/tutorials/launch-into-orbit/launch_into_orbit.py
   :language: python
   :start-at: burn = circularization_burn(ksa.ut + orbit.time_to_apoapsis)
   :end-at: burn.remove()
