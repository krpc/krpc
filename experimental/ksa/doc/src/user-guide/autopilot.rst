Auto-Pilot
==========

``Vehicle.AutoPilot`` is the game's flight computer, which steers the vehicle to a target
attitude with its reaction wheels, engines and RCS. This script points the active vehicle 30
degrees above the eastern horizon, then prograde:

.. literalinclude:: /scripts/user-guide/autopilot/Point.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

Targets and Modes
-----------------

``Mode`` sets what the auto-pilot points the vehicle at:

* ``Attitude`` points it at ``TargetPitch``, ``TargetHeading`` and ``TargetRoll``. Setting any
  of these, ``TargetPitchAndHeading`` or ``TargetDirection`` selects this mode. A target roll
  of NaN leaves the roll free.
* ``Hold`` holds the vehicle's rotation relative to the stars.
* The other modes track a direction: orbital or surface prograde and retrograde, normal and
  anti-normal, and radial in and out. They follow the direction as it changes.

``Error`` and ``RollError`` give how far the vehicle is from the target, in degrees.
``Wait`` returns once they are within twice the flight computer's deadband.

``TargetDirection`` converts a direction in any frame to a pitch and heading when it is
called, so the target stays fixed relative to the surface. See :doc:`reference-frames`.

Executing Burns
---------------

``AutoBurn`` has the flight computer execute the next planned burn: it points the vehicle
along the burn, fires the engines at the ignition time, and stops when the burn is complete or
the engines run out of propellant. :doc:`/tutorials/circularize` walks through it.

Manual Rotation
---------------

KSA's pitch, yaw and roll inputs are on and off flags, which the game clears unless the player
is flying the vehicle. A script steers with the auto-pilot instead.
