C# Client Design
================

The internal design of the C# client. This page builds on the :doc:`shared client design
</client-design>` and the :doc:`client specification </client-spec>`. Usage is in
the :doc:`C# guide </csharp>`. Paths are relative to ``client/csharp/``.

Generated Stubs and Target Framework
------------------------------------

``clientgen`` writes ``Services/<Service>.cs`` from ``csharp.tmpl`` in ``krpctools``, compiled into
one ``netstandard2.0`` assembly. A .NET 5 or later project references it with no compatibility shim.
``UnixEndPoint`` exists because ``netstandard2.0`` has no endpoint for unix domain sockets. It
passes the local socket tests on .NET 10, so the client has no separate ``net10.0`` assembly.
The tests run on .NET 10, and none run on .NET Framework or Mono.

* A service is an extension method of ``IConnection``. Stubs use only that interface.
* Constructing a service registers its exception types on that connection. ``Connection`` registers
  the ``KRPC`` ones itself. Types with a ``System`` equivalent are a table in ``GetException``,
  which adds no public member to ``IConnection``. The argument exceptions also take their arguments
  in different orders.

Streams from Expressions
------------------------

``AddStream`` takes an expression tree, and reads the procedure from the stub's ``RPCAttribute``:

* A single call or property read with fixed arguments streams that call. ``GetCall`` evaluates the
  object and the arguments once, on the client. Conversions to its own type and multiplications by
  one, which F# units of measure add, still count as a single call.
* Conversions of a ``KRPC`` service call run on the client, as a function cannot call that service.
* ``FunctionCompiler`` translates any other lambda into a server side function, one call per node.

A C# type cannot say that a reference-typed position inside a collection is nullable. ``GetCall``
reads the spec from ``RPCAttribute.Types``, the service's generated ``TypeSpecs`` class. A compiled
lambda holding a reference inside a collection or tuple asks the server for its return type.

Connections carried by the lambda's objects, its services, a static method's argument and a
``Bound`` object are not used or checked.

Ownership
---------

``Connection`` owns the socket and two threads. Services, objects, streams and events each hold it,
and stay valid objects after it closes:

.. code-block:: csharp

   using (var connection = new Connection ()) {
       vessel = connection.SpaceCenter ().ActiveVessel;
       met = connection.AddStream (() => vessel.MET);
   }
   met.Get ();     // Throws ConnectionException
   _ = vessel.MET; // Throws ConnectionException

References
^^^^^^^^^^

The connection holds its ``Session`` and ``StreamManager``. The ``Session`` holds the ``Transport``.

The objects the program keeps also hold the connection. A ``Service`` or ``RemoteObject`` holds the
``Connection``. A ``Stream`` or ``Event`` holds its ``StreamImpl``, which holds the ``Connection``
and its callbacks.

The threads keep the connection alive. The reader and dispatcher threads hold the ``Session``, which
holds the ``Connection`` and the ``StreamManager``. An open connection is therefore always
reachable.

The stream manager holds the ``Connection``, and each open ``StreamImpl`` strongly. A stream with no
handle therefore keeps running, on the server and in its callbacks, until ``Remove`` or the close.
It holds every registered ``StreamImpl`` weakly, which keeps nothing alive.

The garbage collector handles the cycles, so a callback can hold anything.

Lifetime
^^^^^^^^

* ``Dispose`` closes the connection. A second ``Dispose`` does nothing. ``Remove`` and setting
  ``Rate`` on a stream it ended do nothing.
* Once a read or write finds a failure, the threads end and the socket closes with no ``Dispose``.
* A connection dropped with no ``Dispose`` stays open until the program exits. The threads are
  background threads, so they do not delay the exit.
* The finalizer closes nothing. It runs only once the socket has closed.

Threads
-------

The threads follow the :doc:`shared design </client-design>`, and are named
``krpc-reader`` and ``krpc-dispatcher``.

* The role's holder decodes and stores stream values under the update lock.
* A ``krpc-waker`` thread starts when the connection ends and another thread holds a condition, and
  ends once the holder lets go.
* Lock order is a stream's condition, then the update lock.
* An exception from a callback goes to ``Console.Error``.
* The dispatcher thread gives up taking a condition once a close is requested.
* An interrupt pending when ``Dispose`` starts is raised when it returns, so the close runs in full.

Streams and Events
------------------

``Stream<T> : Stream`` follows ``Task<T> : Task``. The base holds everything except ``new T Get ()``
and ``AddCallback (Action<T>)``, so a stream with no value is a plain ``Stream``. ``Event`` holds a
``Stream<bool>``.

Mapping the Spec to C#
----------------------

.. list-table::
   :header-rows: 1

   * - Spec
     - C#
   * - Closing a connection
     - ``Connection.Dispose``, or a ``using`` block
   * - Remote error
     - ``RemoteException``, an ``Exception``
   * - Connection, stream, encoding error
     - ``ConnectionException``, ``StreamException``, ``EncodingException``
   * - Server error with no mapped type
     - ``ServerException``, the base of each exception class a service declares
   * - Server exception with a mapped type
     - The .NET exception itself, from the table in ``lang/csharp.py`` in ``krpctools``. The stubs
       generate no class for it, and register a function that creates it
   * - Nullable value
     - ``T?`` for a value type, and a reference type holds null. A generated ``TypeSpec`` names the position
   * - Holding a condition
     - ``lock (stream.Condition)``. ``Wait`` without it throws ``SynchronizationLockException``
   * - Interrupting a blocked thread
     - ``Thread.Interrupt``. A call in a socket read is not interrupted
   * - Dropping the last handle
     - None. ``Stream`` and ``Event`` have no finalizer, so dropping every handle leaves them running
   * - Scoped stream or event
     - A ``using`` block. ``Dispose`` calls ``Remove``, and ignores a failed connection, as the server removes the stream with the connection and ``using`` keeps no second exception
   * - Flags enumeration
     - An ``enum`` with ``[Flags]``. C# cannot define operators on an ``enum``, so ``~`` sets the undeclared
       bits too
