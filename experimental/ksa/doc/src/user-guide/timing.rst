Timing
======

The server runs once per frame, between the game's vehicle solver steps. Calls read the state
of the last step, and a change takes effect in the next one. This script sets the throttle,
then waits for a stream of the thrust to change:

.. literalinclude:: /scripts/user-guide/timing/Throttle.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

Calls That Wait
---------------

Some changes go through the game's input events, which apply at the start of the next frame.
The procedures that make them return once the change has applied, so the next call sees it:

* The part module actions, such as ``Engine.Active``, ``Decoupler.Decouple`` and
  ``Parachute.Deploy``.
* ``KSA.LaunchVehicle``, which returns once the vehicle is on the launch site.
* ``Vehicle.AddBurn``, which returns once the flight computer has timed the next burn.

``KSA.WarpTo`` returns once the warp has finished. The flight computer executes a burn only at
normal speed, so warp to shortly before it.

Predictions
-----------

The game predicts trajectories, impacts and the performance of a vehicle's sequences on worker
threads. ``Burn.Orbit``, ``Orbit.NextOrbit`` and ``Flight.TimeToImpact`` reflect a change to
the trajectory shortly after it. The service recomputes ``Sequence`` performance itself, at
most once per game time, as the game does so only while its windows are open.
