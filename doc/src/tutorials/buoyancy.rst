.. currentmodule:: SpaceCenter

Boats and Submarines
====================

A hull floats when the water it pushes aside weighs more than the hull does.
:attr:`Flight.displacement` is the volume of water the vessel pushes aside when it is
fully submerged, and :meth:`Flight.buoyant_force_at` is the force that volume pushes back
with:

.. code-block:: python

   body = vessel.orbit.body
   flight = vessel.flight()
   displacement = flight.displacement
   buoyancy = flight.buoyant_force_at(body.ocean_density, body.surface_gravity)

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

.. code-block:: python

   flight = vessel.flight(vessel.surface_reference_frame)
   center_of_buoyancy = flight.center_of_buoyancy
   center_of_mass = flight.center_of_mass

:attr:`Flight.center_of_buoyancy` is ``None`` when no part of the vessel is in the water.
The same pair is available in the editor as :meth:`EditorVessel.center_of_buoyancy` and
:meth:`EditorVessel.center_of_mass`, so a hull can be trimmed before it is launched.

The couple is also available as a torque about the center of mass. A hull trimmed level
carries zero torque:

.. code-block:: python

   torque = flight.buoyant_torque_at(body.ocean_density, body.surface_gravity)

:meth:`Flight.buoyant_torque_at` is the torque with the hull fully submerged. It holds
steady while the hull rides the waves, where :attr:`Flight.buoyant_torque` follows the
live waterline. :meth:`EditorVessel.buoyant_torque_at` gives the same figure in the editor.

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

.. code-block:: python

   for part in vessel.parts.all:
       if part.static_pressure + part.dynamic_pressure > part.max_pressure:
           print(part.title, "is over its pressure limit")

:meth:`CelestialBody.pressure_at` takes a negative altitude for a point under the sea, to
plan how deep a dive can go.

Example
-------

This script reports whether the active vessel floats, and how far its center of buoyancy
sits from its center of mass:

.. literalinclude:: /scripts/tutorials/BuoyancyTrim.py
   :language: python
