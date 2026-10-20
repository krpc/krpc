.. currentmodule:: SpaceCenter

Boats and Submarines
====================

A hull floats when the water it pushes aside weighs more than the hull does.
:attr:`Flight.displacement` is the volume of water the vessel pushes aside when it is
fully submerged, and :meth:`Flight.buoyant_force_at` is the force that volume pushes back
with:

.. literalinclude:: /scripts/tutorials/buoyancy/BuoyantForce.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]

Compare that force with the vessel's weight. A hull with more buoyancy than its weight
floats. A submarine trims its tanks to move between the two.
:attr:`Part.displacement` and :meth:`Part.buoyant_force_at` give the same figures for one
part.

.. note::

   :meth:`Flight.buoyant_force_at` applies :attr:`SpaceCenter.buoyancy_scalar`, which is 1.2
   in stock KSP. A buoyant force in the game is a fifth larger than
   `Archimedes' principle <https://en.wikipedia.org/wiki/Archimedes%27_principle>`_ alone
   gives.

Trim
----

A hull at rest carries its center of buoyancy on the same vertical line as its center of
mass. A horizontal offset between the two turns the weight and the buoyancy into a couple
that rolls or pitches the hull. That offset is the trim.

Read both in the vessel's surface reference frame. Its x axis points up, so the trim is
what the other two axes carry:

.. literalinclude:: /scripts/tutorials/buoyancy/CenterOfBuoyancy.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]

:attr:`Flight.center_of_buoyancy` is ``None`` when no part of the vessel is in the water.

The couple is also available as a torque about the center of mass. A hull trimmed level
carries zero torque:

.. literalinclude:: /scripts/tutorials/buoyancy/BuoyantTorque.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]

:meth:`Flight.buoyant_torque_at` is the torque with the hull fully submerged. It holds
steady while the hull rides the waves, where :attr:`Flight.buoyant_torque` follows the
live waterline.

In the Editor
-------------

A hull can be trimmed before it is launched. :meth:`EditorVessel.flight` returns a
:class:`SimulatedFlight`, which has the fully submerged members of :class:`Flight`. Its
:attr:`SimulatedFlight.surface_reference_frame` sets which way is up. Here a hull built
nose-up in the vehicle assembly building is simulated floating on its bottom:

.. literalinclude:: /scripts/tutorials/buoyancy/EditorTorque.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]

Gravity acts along the negative x axis of the surface reference frame. A frame created
relative to :attr:`EditorVessel.reference_frame` follows the hull as it is edited.

Submersion
----------

:attr:`Flight.submerged_portion` is the fraction of the vessel below the waterline, and
:attr:`Flight.depth` is how far its center of mass sits below the surface. The same pair
is available per part, alongside :attr:`Part.splashed` and the depth of the part's
shallowest and deepest points.

Pressure
--------

Below the waterline, :attr:`Flight.static_pressure` and :attr:`Part.static_pressure`
include the weight of the water. With part pressure limits enabled in the difficulty
settings, a part is destroyed when its pressure exceeds :attr:`Part.max_pressure`:

.. literalinclude:: /scripts/tutorials/buoyancy/PressureLimits.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]

The loop reads the parts with separate calls, which can land in different physics ticks. A
:ref:`server side function <tutorial-server-side-functions>` reads every part in the same tick,
with a single call:

.. literalinclude:: /scripts/tutorials/buoyancy/PressureLimitsFunction.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]

:meth:`CelestialBody.pressure_at` takes a negative altitude for a point under the sea, to
plan how deep a dive can go.

Example
-------

This script reports whether the active vessel floats, and how far its center of buoyancy
sits from its center of mass:

.. literalinclude:: /scripts/tutorials/buoyancy/BuoyancyTrim.py
   :language: python
