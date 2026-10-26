Executing a Burn
================

This tutorial circularizes a vehicle's orbit at its apoapsis. The script plans the burn,
and the vehicle's flight computer executes it, as when the player plans a burn in the game.
The vehicle needs an engine and the propellant for the burn.

The complete script is :download:`circularize.py </scripts/circularize.py>`.

Planning the Burn
-----------------

The vis-viva equation gives the speed at apoapsis now, and the speed of a circular orbit
there. We plan a prograde burn of the difference at the apoapsis:

.. literalinclude:: /scripts/circularize.py
   :language: python
   :start-at: mu = orbit.body
   :end-at: print(f"Circularization burn

The flight computer works out the burn time from the vehicle's engines, and centers the
burn on its time.

Executing the Burn
------------------

We warp to 30 seconds before ignition, and turn on the auto-burn. The flight computer points
the vehicle along the burn, fires the engines at the ignition time and stops once the burn is
complete, and then turns the auto-burn off. An event, a :doc:`server side function
</user-guide/server-side-functions>`, waits for that:

.. literalinclude:: /scripts/circularize.py
   :language: python
   :start-at: ksa.warp_to
   :end-at: burn.remove()

The flight computer keeps the last burn it completes, so we remove it.
