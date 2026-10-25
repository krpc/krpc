Raising an Orbit
================

This tutorial raises a vehicle from its circular orbit to a higher circular orbit, with two
burns. The first burn raises the apoapsis to the target altitude. The second burn, at the
apoapsis, raises the periapsis to match. The script flies the active vehicle, which needs
an engine and the propellant for both burns.

The complete script is :download:`raise_orbit.py </scripts/raise_orbit.py>`.

Pointing the Vehicle
--------------------

We point the vehicle prograde with its auto-pilot, and wait until it faces that way:

.. literalinclude:: /scripts/raise_orbit.py
   :language: python
   :start-at: auto_pilot.mode =
   :end-at: auto_pilot.wait()

The auto-pilot is the game's flight computer. It keeps tracking prograde while the
engines burn.

Raising the Apoapsis
--------------------

We light the engines at full throttle, and wait for the apoapsis to reach the target. The
event is a server side function, which the server evaluates each frame:

.. literalinclude:: /scripts/raise_orbit.py
   :language: python
   :start-at: control.throttle = 1
   :end-before: # The circularization burn

Planning the Second Burn
------------------------

The vis-viva equation gives the speed at apoapsis now, and the speed of a circular orbit
there. The difference is the change in velocity the burn needs:

.. literalinclude:: /scripts/raise_orbit.py
   :language: python
   :start-at: mu = orbit.body
   :end-at: delta_v = v2 - v1

The rocket equation turns that into a burn time, from the vehicle's mass, thrust and
specific impulse:

.. literalinclude:: /scripts/raise_orbit.py
   :language: python
   :start-at: exhaust_velocity =
   :end-at: burn_time = (initial_mass

Coasting to Apoapsis
--------------------

We warp to 10 seconds before the burn, which is centered on the apoapsis, and then wait
for the burn to start:

.. literalinclude:: /scripts/raise_orbit.py
   :language: python
   :start-at: burn_ut =
   :end-at: burn_start.wait()

Raising the Periapsis
---------------------

The second burn ends at its planned time, or when the periapsis reaches the target,
whichever is first:

.. literalinclude:: /scripts/raise_orbit.py
   :language: python
   :start-after: print("Raising periapsis")
   :end-at: auto_pilot.disengage()
