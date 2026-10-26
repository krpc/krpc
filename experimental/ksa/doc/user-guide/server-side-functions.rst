Server Side Functions
=====================

A server side function is code that runs inside the game, on the server. The Python client
compiles a lambda or function into one. It has three main uses:

* **Custom events** wait for a condition to become true. The server checks the condition on
  every update, and sends your script a message when it fires. Polling from your script adds a
  round trip to every check, and can miss a condition that holds for only a few frames.
* **Function streams** stream the result of a computation over several values. The server reads
  every value in the same frame, so the values are consistent.
* **Run-once functions** run a sequence of calls in a single frame, from a single call. The same
  calls made from your script take a round trip each, and the game advances between them.

This event fires when the vehicle's apoapsis passes 100 km:

.. literalinclude:: /scripts/user-guide/server-side-functions/FirstEvent.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The server runs once per frame, so it checks the event once per frame. See :doc:`timing`.

Captured Values
---------------

The client splits a function by what each part depends on. A property or method of a remote
object becomes part of the function, and the server reads it on every evaluation. A part that
uses only values from your script, such as a captured variable, is evaluated once, when the
function is compiled:

.. literalinclude:: /scripts/user-guide/server-side-functions/CapturedValue.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The client computes ``threshold * 2`` when it creates the event, so the server compares the
apoapsis with the constant 100000. The chain ``vehicle.orbit.apoapsis_altitude`` is part of the
function, so it follows the vehicle's current orbit.

Computed Streams
----------------

A stream of a function streams its computed value. This streams the vehicle's thrust to weight
ratio, from its thrust and mass read in the same frame:

.. literalinclude:: /scripts/user-guide/server-side-functions/ComputedStream.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

A function can produce a collection. This streams the thrust of each of the vehicle's engines:

.. literalinclude:: /scripts/user-guide/server-side-functions/EngineThrusts.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

Here ``engines`` is a captured variable, so the list of engines is fixed when the function is
compiled. The server reads only their thrusts.

Collections and Statements
--------------------------

Python's collection operations, such as ``any``, ``all``, ``sum`` and comprehensions, compile to
the server's collection operations. This event fires when an active engine runs out of
propellant, such as the Rocket's sea level engine about a minute after it lifts off:

.. literalinclude:: /scripts/user-guide/server-side-functions/OutOfPropellant.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

A function can contain statements, loops and local variables before its return. This streams
the fraction of the vehicle's propellant that is left:

.. literalinclude:: /scripts/user-guide/server-side-functions/PropellantFraction.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

Run-Once Functions
------------------

A function can also change the game, by setting properties and calling methods. ``conn.run``
runs it once, with every step in the same frame. This points a vehicle on the launch pad
straight up and lights its engines:

.. literalinclude:: /scripts/user-guide/server-side-functions/RunOnce.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

.. note::

   An event or stream runs its function on every update, side effects included. Keep event and
   stream functions free of side effects.

Procedures That Wait
--------------------

A function runs within a single frame, so it cannot call a procedure that waits for a later
frame. The call raises an error, and the effects the procedure made before it waited still
happen. These procedures wait:

* ``KSA.WarpTo`` and ``KSA.LaunchVehicle``.
* ``Vehicle.AddBurn`` and ``AutoPilot.Wait``.
* ``Control.ActivateNextSequence``.
* The part module setters and actions, such as ``Engine.Active``, ``Decoupler.Decouple`` and
  ``Parachute.Deploy``.

Call these from your script, between functions.

More
----

The `kRPC server side functions guide
<https://krpc.github.io/krpc/@KRPC_VERSION@/tutorials/server-side-functions.html>`_ covers the
rest of what a function can do: arguments, state kept between updates, strings, exceptions,
printing, and the expression API that clients without a compiler use.
