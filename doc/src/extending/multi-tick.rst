.. _service-api-multi-tick:

Multi-Tick RPCs
===============

By default kRPC executes each RPC call within a single physics tick. Some operations need to wait
for the game to process one or more physics updates before their result is available — for example,
staging only takes effect after the game runs its next ``FixedUpdate``. For these cases a procedure can
yield, by throwing a ``KRPC.Service.YieldException`` to hand a *continuation* back to
kRPC. kRPC suspends the call, allows the next physics tick to run, then invokes the continuation.
The continuation can itself throw another ``YieldException`` to yield again, forming a chain. The
client blocks throughout and receives the final return value (or exception) once the chain
completes.

.. cs:class:: KRPC.Service.YieldException<T>

   Thrown by a procedure or method to suspend execution until the next physics tick. ``T`` must be
   a delegate type. kRPC invokes the delegate on the following ``FixedUpdate`` and uses its return
   value as the result of the RPC call. If the delegate itself throws a ``YieldException``, kRPC
   will yield again.

   The return type of the delegate must match the return type of the RPC procedure or method:

   * ``Action`` for a ``void`` procedure.
   * ``Func<T>`` for a procedure that returns type ``T``.

Examples
--------

**Simple retry** — re-invoke the same method on each tick until a condition is met:

.. literalinclude:: /scripts/extending/multi-tick/YieldRetry.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

Each physics tick kRPC calls ``Wait`` again. As long as the condition is not met, the method
yields again by passing itself as the next continuation. Once the condition is satisfied the method
returns normally and the client receives its response.

**Two-phase with closure** — trigger an action, then poll a helper until the result is available:

.. literalinclude:: /scripts/extending/multi-tick/YieldDecouple.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

``Decouple`` fires the decoupler and immediately calls ``PostDecouple``. ``PostDecouple`` yields
for at least 10 ticks (to let the game settle the new vessel list) and until the ``Decoupled`` flag
becomes true. The closure captures ``preVesselIds`` and increments ``wait`` on each tick so the
helper has the right context every time it runs.
