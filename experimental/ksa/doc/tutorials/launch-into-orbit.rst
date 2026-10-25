Launching into Orbit
====================

This tutorial launches the game's Rocket from Cape Canaveral into a circular orbit
200 km up. The script flies the ascent, stages when the first stage runs out of
propellant, and has the flight computer execute the circularization burn.

The complete script is :download:`launch_into_orbit.py </scripts/launch_into_orbit.py>`.

The Ascent Profile
------------------

Earth's atmosphere is deep, so the rocket climbs most of the way through it before it
flies level. The gravity turn starts at 1 km and ends at 150 km, and the target apoapsis is
above the top of the atmosphere:

.. literalinclude:: /scripts/launch_into_orbit.py
   :language: python
   :start-at: TURN_START_ALTITUDE =
   :end-at: TARGET_ALTITUDE =

Launching
---------

We launch the vehicle on the pad, point it straight up and light the first stage:

.. literalinclude:: /scripts/launch_into_orbit.py
   :language: python
   :start-at: vehicle = ksa.launch_vehicle
   :end-at: vehicle = ksa.launch_vehicle

.. literalinclude:: /scripts/launch_into_orbit.py
   :language: python
   :start-at: auto_pilot.target_pitch_and_heading(90, 90)
   :end-at: control.engine_on = True

The Gravity Turn
----------------

The loop pitches the rocket over toward the east as it climbs, until the apoapsis reaches
the target. The pitch follows a square root of the altitude, so the rocket turns fastest
low down:

.. literalinclude:: /scripts/launch_into_orbit.py
   :language: python
   :start-at: while apoapsis() < TARGET_ALTITUDE:
   :end-at: control.engine_on = False

A stage that has run dry has no thrust available. Staging activates the next two
sequences, the decoupler that drops the spent stage and the engine of the next one:

.. literalinclude:: /scripts/launch_into_orbit.py
   :language: python
   :start-at: def stage():
   :end-before: def execute_burn

Circularizing
-------------

Once the rocket is out of the atmosphere, we plan a burn at the apoapsis that makes the
orbit circular, as in :doc:`circularize`, and warp to it:

.. literalinclude:: /scripts/launch_into_orbit.py
   :language: python
   :start-at: mu = body.gravitational_parameter
   :end-at: ksa.warp_to

The first stage can run dry during the burn. The flight computer then stops, and we stage
and continue the burn:

.. literalinclude:: /scripts/launch_into_orbit.py
   :language: python
   :start-at: def execute_burn(burn):
   :end-at: stage()
