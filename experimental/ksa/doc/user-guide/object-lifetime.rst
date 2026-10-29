Object Lifetime
===============

Many procedures return an object: a vehicle, a part, a burn, an orbit. Each identifies
something in the game, and every call made through it reads that game state. This script
fires the lowest decoupler of the active vehicle, which returns the vehicles it separated, then
finds the part it separated:

.. literalinclude:: /scripts/user-guide/object-lifetime/Decouple.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

A call through an object whose game object is gone raises ``KRPC.ObjectDestroyedException``,
and the server frees the object.

Vehicles
--------

A vehicle object holds the game's vehicle, as a KSA vehicle has no permanent id. Renaming the
vehicle keeps the object. The object raises an exception once the vehicle leaves the system:

* Docking and undocking replace the vehicles involved.
* Loading a save replaces every vehicle.
* A vehicle destroyed in a crash is gone.

Separating a vehicle with a decoupler keeps its object, which holds the side with the root
part. The other side is a new vehicle, with a number appended to the name.

Parts and Modules
-----------------

A part keeps its object when the vehicle it belongs to changes, so ``Part.Vehicle`` follows a
part that a decoupler separates. A part object raises an exception once the part no longer
exists.

Burns
-----

A burn object raises an exception once the burn is removed. The flight computer removes a burn
it completes when another burn follows it, and keeps the last one.

Orbits
------

A vehicle's ``Orbit`` follows the vehicle as its trajectory changes. ``NextOrbit`` and
``Burn.Orbit`` are the game's prediction at the time they are obtained. The game updates its
prediction on a worker thread, so get them again for an updated prediction. Bodies and their
orbits last as long as the system.
