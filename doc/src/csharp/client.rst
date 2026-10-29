.. default-domain:: cs
.. highlight:: csharp
.. namespace:: KRPC.Client

C# Client
=========

This guide describes how to install the C# client, and the features it provides. The
:ref:`csharp-client-reference` describes each function and class.

Installing the Library
----------------------

The library is a NuGet package, available from `nuget.org
<https://www.nuget.org/packages/KRPC.Client>`_ or `downloaded from GitHub
<https://github.com/krpc/krpc/releases>`_. It requires .NET Framework 4.7.2 or later.

To install using the .NET CLI:

.. code-block:: bash

   dotnet add package KRPC.Client

NuGet also installs the package's dependency, ``Google.Protobuf``.

Connecting to the Server
------------------------

The game must be running with the server started. :ref:`getting-started-server` in the
getting started guide shows how to install and start it.

The client connects over TCP/IP or over a local socket. Each server in the server window uses one
protocol, and accepts connections over that protocol only. Pick the protocol when you add the
server, and connect with the matching function.

.. note::

   A server with "Auto-accept new clients" turned off answers only once the player approves the
   connection in game. The connection fails after 5 seconds by default. Pass
   ``Timeout.InfiniteTimeSpan`` as the ``handshakeTimeout`` to wait indefinitely for the player.

Over TCP/IP
^^^^^^^^^^^

Select "Protobuf over TCP/IP" as the server's protocol. Constructing a
:type:`~KRPC.Client.Connection` connects to it. You make every call through this object. The
following connects to a server with the default settings, address ``127.0.0.1`` and port 50000,
and prints its version:

.. literalinclude:: /scripts/csharp/client/Connecting1.cs

The constructor also takes the address and port of the server, and a name for the connection. These
match the address and port shown in the server window. The address is an :type:`IPAddress
<System.Net.IPAddress>`. The name appears in the server window in the game:

.. literalinclude:: /scripts/csharp/client/Connecting2.cs

TCP/IP works over a network, so your program can run on a different machine from the game.

Over a Local Socket
^^^^^^^^^^^^^^^^^^^

Select "Protobuf over local socket" as the server's protocol.
:meth:`~KRPC.Client.Connection.ConnectLocal` connects to it, and returns a
:type:`~KRPC.Client.Connection` as the constructor does. The following connects to a server with
the default settings, at the default socket path for your platform, and prints its version:

.. literalinclude:: /scripts/csharp/client/ConnectingLocal.cs

:meth:`~KRPC.Client.Connection.ConnectLocal` also takes a ``path``, for a server with another
socket path. The path matches the one shown in the server window.

A local socket works only on the machine running the game. It has lower overhead than TCP/IP, so
it is faster.

Closing the Connection
^^^^^^^^^^^^^^^^^^^^^^

Use the connection in a ``using`` statement to close it at the end of the block:

.. literalinclude:: /scripts/csharp/client/Connecting3.cs

:meth:`~KRPC.Client.Connection.Dispose` closes a connection explicitly.

Calling Remote Procedures
-------------------------

The server provides *procedures* for your program to call, grouped into *services*.

The following gets the active vessel from
:prop:`~KRPC.Client.Services.SpaceCenter.SpaceCenter.ActiveVessel`, an object of type
:type:`~KRPC.Client.Services.SpaceCenter.Vessel`. It sets the vessel's name, then prints its
altitude:

.. literalinclude:: /scripts/csharp/client/RemoteProcedures.cs

Each service is an extension method of the connection, such as ``connection.SpaceCenter()`` for
the :type:`~KRPC.Client.Services.SpaceCenter.SpaceCenter` service. The method and the service's
classes are in a namespace for the service, such as :doc:`KRPC.Client.Services.SpaceCenter
</csharp/api/space-center>`. Your editor's code completion lists the classes, enumerations,
procedures and properties of a service.

Handling Errors
---------------

A procedure that fails on the server throws an exception in your program. The following catches
the error from loading a save that does not exist:

.. literalinclude:: /scripts/csharp/client/Errors.cs

The exception you catch depends on the error:

* A standard error, such as an invalid argument, throws the matching .NET exception from the
  ``System`` namespace. The example catches :type:`System.ArgumentException`, which a missing save
  throws.
* An error particular to a service throws a class of that service, such as
  :type:`~KRPC.Client.Services.KRPC.ObjectDestroyedException`.
* An error the client has no class for throws :type:`~KRPC.Client.RPCException`.
* A call made once the connection has closed or failed throws
  :type:`~KRPC.Client.ConnectionException`.

Remote Objects
--------------

Many procedures return an object identifying something in the game, such as a vessel or a part.
The object holds no copy of the game's state. It is just a reference to it. Each call through it
reads the game as it is now:

.. literalinclude:: /scripts/csharp/client/RemoteObjects.cs

Two objects for the same thing in the game compare equal, and can be used as dictionary keys.

Once the thing an object identifies is destroyed, such as a part that explodes, each call through
it throws :type:`~KRPC.Client.Services.KRPC.ObjectDestroyedException`.

An object for a vessel or a part keeps working when you load a save, quickload or revert, and
then reads the loaded game. Other objects depend on what they identify. A maneuver node, for
example, is destroyed by a load.

A vessel far from the active vessel is *unloaded*, and the game keeps no parts for it. The vessel
itself reads normally, but a call on one of its parts throws
:type:`~KRPC.Client.Services.KRPC.ObjectNotLoadedException`. The part works again once the vessel
comes back into range.

.. seealso:: :doc:`/user-guide/object-lifetime` describes how each kind of object behaves in more
             detail.

.. _csharp-client-streams:

Streaming Data
--------------

A common use of kRPC is to read data from the game continuously. The following does so by calling
a remote procedure in a loop, printing the position of the active vessel:

.. literalinclude:: /scripts/csharp/client/Streaming1.cs

Each call sends a request to the server and waits for its response. A *stream* is more efficient.
The server runs the call repeatedly, and sends your program the result each time it changes.

The following does the same as above using a stream:

.. literalinclude:: /scripts/csharp/client/Streaming2.cs

:meth:`~KRPC.Client.Connection.AddStream` creates the stream from a lambda expression that makes
the call, and returns a :type:`~KRPC.Client.Stream`. Calling :meth:`~KRPC.Client.Stream.Get`
returns the stream's latest value.

A property is streamed with a lambda that reads it, such as ``() => flight.MeanAltitude``.

A lambda that makes one call with fixed arguments streams that call. The object the call is made
on is evaluated once, when the stream is created. Any other lambda is compiled into a
:ref:`server side function <csharp-client-compiling-lambdas>`, which the server evaluates on each
update.

A stream can be made of any procedure or property that returns a value. Each call to
:meth:`~KRPC.Client.Connection.AddStream` creates another stream.

A stream runs until you remove it with :meth:`~KRPC.Client.Stream.Remove`, your program drops its
last reference to it, or the connection closes. Keep a reference to a stream for as long as you
need its values or its callbacks.

By default, the server sends a stream's value each time it changes. Set
:prop:`~KRPC.Client.Stream.Rate` to limit how many times a second the stream updates:

.. literalinclude:: /scripts/csharp/client/StreamRate.cs

Waiting for Changes
^^^^^^^^^^^^^^^^^^^

Each stream has a condition variable, an object you lock with the ``lock`` statement.
:meth:`~KRPC.Client.Stream.Wait` blocks until the stream's value changes. The following waits for
the abort button to be pressed:

.. literalinclude:: /scripts/csharp/client/ConditionVariables.cs

Lock the condition when you call :meth:`~KRPC.Client.Stream.Wait`, as the ``lock`` statement does
here. While you hold it, the value changes only when you wait, so the loop misses no updates. An
ending reaches you the same way: the wait returns, and your next read or wait throws the exception
that ended the stream.

:meth:`~KRPC.Client.Stream.Wait` takes a timeout in seconds, and returns ``false`` when it elapses
before an update.

Callbacks
^^^^^^^^^

A callback is a function the client calls with each new value of a stream. The following adds
two callbacks to a stream of the abort button:

.. literalinclude:: /scripts/csharp/client/Callbacks.cs

A callback added to a stream that already has a value is called with that value straight away.
It is then called with each new value, in order.

.. note::

   Each connection has a callback thread, which runs the callbacks of its streams one at a time. A
   callback can make remote procedure calls. Other callbacks and waits are held up while it runs, so
   return promptly, and do not wait on a stream in a callback. For a long running task, start a
   worker thread from the callback.

.. _csharp-client-stream-endings:

Stream Endings
^^^^^^^^^^^^^^

A stream ends when its procedure throws an exception, when the server removes it, when you remove
it, or when the connection closes. Calling :meth:`~KRPC.Client.Stream.Get` or
:meth:`~KRPC.Client.Stream.Wait` then throws the exception that ended it. The following streams
the temperature of a part until the part is destroyed:

.. literalinclude:: /scripts/csharp/client/StreamEnd.cs

The exception is the one the procedure threw, or one of the following:

* :type:`~KRPC.Client.Services.KRPC.StreamRemovedException` when the server removes the stream,
  such as for an :ref:`event <csharp-client-events>` that has finished, or when you remove it.
* :type:`~KRPC.Client.ConnectionException` when the connection closes or fails.

An exception for an object that is not loaded, or for a procedure not available in the current
game scene, does not end the stream. It pauses the stream's updates, and the stream keeps its last
value. The updates resume once the object or the scene is available.

.. seealso:: :doc:`/user-guide/object-lifetime` describes when each kind of object is not loaded,
             and how streams behave around a load.

:meth:`~KRPC.Client.Stream.AddEndCallback` adds a callback that is called once when the stream
ends, with the exception. The following prints the temperature of a part with a callback, and
prints the exception when the stream ends:

.. literalinclude:: /scripts/csharp/client/EndCallback.cs

Server Side Functions
---------------------

A server side function is a lambda expression in your program that runs inside the game. The
client compiles it and sends it to the server, which runs it there. The following reads the
vessel's altitude and speed together:

.. literalinclude:: /scripts/csharp/client/RunFunction.cs

:ref:`csharp-client-compiling-lambdas` describes what a function can contain. The :ref:`server
side functions guide <server-side-functions>` shows how to build larger functions.

Running a Function
^^^^^^^^^^^^^^^^^^

:meth:`~KRPC.Client.Connection.Run` runs a function once, and returns its value. The server runs
the whole function within one physics tick, so every value it reads comes from the same moment in
the game. In the example, the altitude and the speed are consistent with each other, which two
separate calls cannot guarantee.

Use :meth:`~KRPC.Client.Connection.Run` for a function with effects, such as a call of
:meth:`~KRPC.Client.Services.SpaceCenter.Control.ActivateNextStage`. A stream or an event evaluates
its function on every update, which repeats the effects.

Streaming a Function
^^^^^^^^^^^^^^^^^^^^

Pass :meth:`~KRPC.Client.Connection.AddStream` a lambda that does more than make one call, to
stream its value. The server evaluates the function on each update, within one physics tick. The
following streams the vessel's altitude in kilometers:

.. literalinclude:: /scripts/csharp/client/CompiledFunctionStream.cs

A function stream ends on an exception its function throws, just like an
:ref:`ordinary stream <csharp-client-stream-endings>`. Unlike an ordinary stream, it also ends on
:type:`~KRPC.Client.Services.KRPC.ObjectNotLoadedException`, thrown while the game loads or when a
vessel unloads. The server cannot pause a function partway through, and running it again would
repeat its effects.

To keep a function running through a load, catch the exception in the function. A lambda holds a
single expression, so build the function with the :ref:`expression API
<server-side-functions-api>`, and catch the exception with
:meth:`~KRPC.Client.Services.KRPC.Expression.TryCatch`:

.. literalinclude:: /scripts/csharp/client/NotLoadedStream.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

.. _csharp-client-events:

Events
------

A common use of kRPC is to wait for something to happen in the game, such as the vessel reaching
an altitude. An *event* does this efficiently. The server checks a condition, written as a server
side function, and tells your program when it becomes true. The following waits until the
vessel's altitude exceeds 1000 meters:

.. literalinclude:: /scripts/csharp/client/CompiledEvent.cs

:meth:`~KRPC.Client.Connection.AddEvent` creates the event, and :meth:`~KRPC.Client.Event.Wait`
waits for it to fire. The server checks the condition every physics tick, so the event catches a
condition that holds for only a moment, which a loop of calls can miss.

As with a stream, lock the event's condition when you call :meth:`~KRPC.Client.Event.Wait`. A wait
returns straight away if the event has fired since the last wait on the same event object, so a
firing between two waits is not missed.

Event Callbacks
^^^^^^^^^^^^^^^

An event can also call a callback each time it fires. The callback takes no arguments:

.. literalinclude:: /scripts/csharp/client/EventCallback.cs

Callbacks for events run on the connection's callback thread, as the callbacks of a stream do.

Event Endings
^^^^^^^^^^^^^

An event ends in the same ways as a :ref:`stream <csharp-client-stream-endings>`. Waiting on an
event that has ended throws the exception that ended it.

When the event fires just before the server ends it, your next wait returns for that firing first.
Removing the event, dropping your program's last reference to it, or closing the connection, ends
it at once.

:meth:`~KRPC.Client.Event.AddEndCallback` adds a callback that is called once when the event ends,
with the exception, just like an ordinary stream.

Threading
---------

A connection can be shared between threads. Each thread makes calls over it as it would alone,
and each call returns its own result. The following prints the altitude and speed of the vessel
from two threads:

.. literalinclude:: /scripts/csharp/client/Threads.cs

Streams and events can also be used from any thread:

* Several threads can read a stream, or wait on a stream or an event. A change wakes every thread
  waiting for it.
* A thread holding a stream's condition does not stop other threads from making calls.
* Callbacks run on the connection's callback thread, one at a time, and never on your program's
  threads. A callback added to a stream that already has a value is different. Its first call, with
  that value, might run on your thread, before :meth:`~KRPC.Client.Stream.AddCallback` returns.
  Protect state that a callback shares with your program's threads, such as with a ``lock``
  statement.

When one thread closes the connection, or the connection fails, the calls and waits of the other
threads end with :type:`~KRPC.Client.ConnectionException`. A wait on a stream or an event that
another thread removes ends with :type:`~KRPC.Client.Services.KRPC.StreamRemovedException`.

Deprecated Members
------------------

A member of a service can be deprecated, when a newer member replaces it. A deprecated member
still works, but may be removed in a future release, so move to its replacement.

The API reference marks it as deprecated, and names the replacement. The member carries the
``[Obsolete]`` attribute, so the C# compiler warns where you use it, with a message naming the
replacement.

.. _csharp-client-compiling-lambdas:

Writing Server Side Functions
-----------------------------

The client includes a compiler, which converts a lambda expression into a server side function
automatically when you pass it to :meth:`~KRPC.Client.Connection.Run`,
:meth:`~KRPC.Client.Connection.AddStream` or :meth:`~KRPC.Client.Connection.AddEvent`. This
section describes what a function can contain, and how to compile one ahead of time.

Remote Calls and Constants
^^^^^^^^^^^^^^^^^^^^^^^^^^

The compiler translates the lambda's expression tree into a server side function that computes
the same result on the server, inside the game. Each part of the function either runs on the
server or becomes a constant:

* Property accesses and method calls on remote objects and services become calls in the
  function, made again on each evaluation. This includes calls on the elements of a collection
  in a LINQ operator. The procedures of the :type:`~KRPC.Client.Services.KRPC.KRPC` service
  cannot be called. They build and run server side functions and add streams, which a function
  would repeat on every evaluation.
* Reading a field of a structure a service defines reads that field of it on the server.
  Constructing one with ``new`` builds it there. Comparing two with ``<``, ``>``, ``<=`` or
  ``>=`` is an error.
* Captured variables, literals and any sub-expression that makes no remote call are evaluated
  once, when the function is compiled, and embedded as constants. An auto-property is read once
  too, as a captured variable is. A captured value in a string concatenation is formatted on the
  client, as C# formats it.

The compiler throws :type:`~KRPC.Client.FunctionCompilationException` for code that would run on
the client, or only once, when compiling, where you would expect it to run on every evaluation:

* A delegate call, and a call to a method, a constructor, a computed property or an operator
  defined outside the .NET base library and the kRPC client. Call it outside the lambda and use
  its result.
* Passing a delegate, or a value whose type overrides a method such as ``ToString``, to the base
  library, as in ``"a" + obj``, ``list.Select(MyMethod)`` and your own
  :type:`IEnumerable\<T> <System.Collections.Generic.IEnumerable>` or :type:`IComparer\<T>
  <System.Collections.Generic.IComparer-1>` given to LINQ. So is reading ``Lazy<T>.Value``.
* Reading a stream with :meth:`~KRPC.Client.Stream.Get`, as the stream would be read once. Make
  the stream's remote procedure call in the lambda instead.
* A lambda passed to :meth:`~KRPC.Client.Connection.AddEvent` or
  :meth:`~KRPC.Client.Connection.AddStream` that makes no remote procedure call, as its value is
  the same on every update. A function compiled with :meth:`~KRPC.Client.Connection.Compile`
  first is not checked.
* An :type:`System.Action` lambda that makes no remote procedure call, as it has no effect on the
  server.

Operators
^^^^^^^^^

* Operators, conditional expressions, casts and indexing compile to the matching expression
  operators.
* ``&&`` and ``||`` short-circuit on the server. ``&`` and ``|`` evaluate both operands.

Null Values
^^^^^^^^^^^

* Comparing a value with ``null`` tests whether it is null. ``??``, ``HasValue``, ``Value`` and
  ``GetValueOrDefault`` behave as in C#.
* An ordering comparison with a nullable number is false when the number is null. Arithmetic
  and logic on a null throw.

Collections
^^^^^^^^^^^

* ``List``, ``HashSet`` and ``Dictionary`` initializers, ``Tuple.Create``, and a tuple's
  ``Item1`` to ``Item7`` compile to the matching expression operators. A dictionary's ``Keys``
  and ``Values`` are lists of its keys and values.
* The LINQ operators ``Select``, ``Where``, ``SelectMany``, ``Any`` and ``All`` with a
  predicate, ``Count``, ``Sum``, ``Min``, ``Max`` and ``Average`` without one, ``Contains``,
  ``OrderBy``, ``Concat``, ``Skip``, ``Take``, ``First``, ``Last``, ``ElementAt``,
  ``Distinct``, ``Reverse``, ``Union``, ``Intersect``, ``Except``, ``Zip``, ``ToDictionary``,
  ``ToList`` and ``ToHashSet`` compile to the server's collection operations.
* A lambda that evaluates to an :type:`IEnumerable\<T> <System.Collections.Generic.IEnumerable>`
  evaluates to a list. One that evaluates to the :type:`IOrderedEnumerable\<T>
  <System.Linq.IOrderedEnumerable-1>` of ``OrderBy`` is an error. Call ``ToList`` on it.

Library Methods
^^^^^^^^^^^^^^^

* The :type:`System.Math` methods ``Abs``, ``Sign``, ``Min``, ``Max``, ``Clamp``, ``Floor``,
  ``Ceiling``, ``Round``, ``Pow``, ``Sqrt``, ``Exp``, ``Log``, ``Log10``, ``Sin``, ``Cos``,
  ``Tan``, ``Asin``, ``Acos``, ``Atan`` and ``Atan2`` take server side arguments. Integer
  ``Abs``, ``Min``, ``Max`` and ``Clamp`` are exact at their own type. ``Math.Round`` takes the
  ``MidpointRounding.ToEven`` and ``AwayFromZero`` modes, given as a client side value.
* ``Console.WriteLine`` writes a line to the game's log with
  :meth:`~KRPC.Client.Services.StdLib.StdLib.Print`. It formats its arguments as
  ``string.Format`` does. Convert a number computed on the server with
  ``ToString(CultureInfo.InvariantCulture)`` before passing it.
* The string members ``Length``, ``Substring``, ``IndexOf``, ``Contains``, ``StartsWith``,
  ``EndsWith``, ``Replace``, ``ToUpperInvariant``, ``ToLowerInvariant``, ``Trim``,
  ``TrimStart`` and ``TrimEnd`` compile to the server's string operations. So does
  ``string.Join``.
* An interpolated string, ``string.Concat``, and ``string.Format`` with plain ``{0}``
  placeholders compile to a concatenation.

Culture
^^^^^^^

The server formats and compares strings with the invariant culture. A call that depends on the
current culture is an error:

* A number converts to a string with ``ToString(CultureInfo.InvariantCulture)``. Format an
  interpolated string with ``FormattableString.Invariant``, or call ``string.Format`` with
  ``CultureInfo.InvariantCulture``.
* ``IndexOf``, ``StartsWith`` and ``EndsWith`` with a string argument take
  ``StringComparison.Ordinal``.
* ``ToUpper`` and ``ToLower`` take ``CultureInfo.InvariantCulture``.
* ``OrderBy`` with a string key takes ``StringComparer.Ordinal``.

Statements
^^^^^^^^^^

The lambda is a C# expression tree, so C#'s expression tree rules apply. It holds a single
expression, and a call in it gives every argument, including the optional ones. Build a function
with statements, or one that throws or handles exceptions, with the :ref:`expression API
<server-side-functions-api>`.

Differences from C#
^^^^^^^^^^^^^^^^^^^

Some results differ from C#'s, as the server computes with its own operations:

* Integer arithmetic wraps around on overflow, so ``checked`` arithmetic is an error.
* ``==`` and ``!=`` compare tuples and collections by their contents. C# compares them by
  reference.
* ``Math.Round`` takes any number of decimal places, and a negative number rounds to tens,
  hundreds and beyond.
* ``ToString`` of an object computed on the server gives the server's name for its class, which
  differs from the client's.
* A ``double`` converted to a string on the server is formatted by the server's runtime. It can
  print fewer digits than the client, as in ``0.3`` for ``0.30000000000000004``.
* An index out of range in ``list[i]``, ``ElementAt`` or ``Substring`` throws
  :type:`System.IndexOutOfRangeException`, where C# throws
  :type:`System.ArgumentOutOfRangeException`.
* A key repeated in ``ToDictionary`` takes the last of its values, where C# throws.

Unsupported Constructs
^^^^^^^^^^^^^^^^^^^^^^

A construct with no server side equivalent throws :type:`~KRPC.Client.FunctionCompilationException`
naming it. Examples are ``GroupBy``, ``Aggregate``, ``MinBy``, ``MaxBy`` and a format specifier.

Compiling Ahead of Time
^^^^^^^^^^^^^^^^^^^^^^^

Pass a lambda expression to :meth:`~KRPC.Client.Connection.Compile` to compile it into a server
side function. :meth:`~KRPC.Client.Connection.AddEvent`, :meth:`~KRPC.Client.Connection.AddStream`
and :meth:`~KRPC.Client.Connection.Run` take the result, and run it without compiling it again. The
following compiles a function once, and runs it every second:

.. literalinclude:: /scripts/csharp/client/CompiledFunction.cs

Given a lambda, :meth:`~KRPC.Client.Connection.AddEvent` and :meth:`~KRPC.Client.Connection.Run`
compile it again on every call. So does :meth:`~KRPC.Client.Connection.AddStream`, unless the
lambda makes one call with fixed arguments. Its captured variables take their current values each
time. A compiled function keeps the values from when it was compiled. Compiling makes a remote
procedure call per node.

To use a lambda repeatedly, compile it once with :meth:`~KRPC.Client.Connection.Compile` and pass
the result. :meth:`~KRPC.Client.Connection.Compile` also compiles the function on the server, so
its first run is as fast as later ones.

Parameters
^^^^^^^^^^

A lambda passed to :meth:`~KRPC.Client.Connection.Compile` can take up to four parameters.
:meth:`~KRPC.Client.Connection.Run` passes its arguments to them:

.. literalinclude:: /scripts/csharp/client/CompiledFunctionArguments.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

An argument is encoded at its parameter's type, and a number widens as C# widens it. A signed
integer that is not negative converts to an unsigned parameter at least as wide, so a ``long``
converts to a ``ulong`` but not a ``uint``. An array other than an ``object[]``, passed as the
only argument, is the argument of a function with one parameter.

An event or a function stream runs its function with no arguments. Passing
:meth:`~KRPC.Client.Connection.AddStream` a lambda with parameters throws
:type:`~KRPC.Client.FunctionCompilationException`.

Service Stubs
-------------

The client includes code for each service that ships with kRPC, generated from the services of
one kRPC release and compiled into ``KRPC.Client.dll``. These *stubs* are the classes and extension
methods in the ``KRPC.Client.Services`` namespaces.

The stubs match the server from the same release, so install the client version that matches your
server. With another version, a procedure that was removed or renamed throws an exception when you
call it. A parameter or return type that changed is not checked, so a call can send or return a
wrong value with no error.

A service with no stub, such as one added by another mod, needs stubs of its own.
:ref:`service-api-clientgen` shows how to generate them.

.. _csharp-client-reference:

Client API Reference
--------------------

.. class:: Connection

   A connection to the server. Each service is an extension method of it, such as
   ``SpaceCenter()``. Disposing of it closes the connection.

   The :type:`~KRPC.Client.Services.KRPC.KRPC` service, from ``KRPC()``, provides the server's
   basic functionality. The client calls some of its procedures itself, such as to add and remove
   streams.

   .. function:: Connection(string name = "", System.Net.IPAddress address = null, int port = 50000, System.TimeSpan timeout = default, System.TimeSpan? handshakeTimeout = null, System.TimeSpan? stallTimeout = null)

      Connects to a kRPC server over TCP/IP.

      :parameters:

       * **name** -- A name for the connection, shown in the server window in the game.
       * **address** -- The address of the server. Defaults to ``127.0.0.1`` when null.
       * **port** -- The port of the server. Defaults to 50000.
       * **timeout** -- How long to wait for the connection to open. Defaults to zero, which waits
         indefinitely. Without a timeout, a network that drops the connection attempt leaves the
         client waiting.
       * **handshakeTimeout** -- How long to wait for the server to accept the connection.
         Defaults to 5 seconds when null. ``Timeout.InfiniteTimeSpan`` waits indefinitely, such
         as for a server that asks the player to approve each connection.
       * **stallTimeout** -- How long a message from the server can go without a new byte, once it
         has started to arrive, before the connection fails. Defaults to 30 seconds when null.
         ``Timeout.InfiniteTimeSpan`` waits indefinitely.

      Throws :type:`~KRPC.Client.ConnectionException` if the connection fails or the server refuses
      it. Throws :type:`System.ArgumentOutOfRangeException` for a negative timeout, or a handshake
      or stall timeout that is neither positive nor infinite.

   .. function:: static Connection ConnectLocal(string name = "", string path = "", System.TimeSpan? handshakeTimeout = null, System.TimeSpan? stallTimeout = null)

      Connects to a kRPC server on the same machine over a local socket. The server must use the
      local socket protocol.

      Local sockets are available on Linux, macOS, and Windows 10 1803 and Windows Server 2019 or
      later.

      :parameters:

       * **name** -- A name for the connection, shown in the server window in the game.
       * **path** -- The path of the server's socket, shown in the server window in the game.
         An empty path, the default, is the one the server uses unless it was configured with
         another.
       * **handshakeTimeout** -- As for the constructor.
       * **stallTimeout** -- As for the constructor.

      Throws :type:`~KRPC.Client.ConnectionException` and :type:`System.ArgumentOutOfRangeException`
      as the constructor does.

   .. function:: void Dispose()

      Closes the connection. Disposing of a connection that is already closed does nothing.

   .. function:: Stream<TResult> AddStream<TResult>(Expression<Func<TResult>> expression)

      Creates a stream from a lambda expression with no parameters, and returns a
      :type:`~KRPC.Client.Stream`. Each call creates a new stream. A lambda that makes one remote
      procedure call with fixed arguments streams that call. Any other lambda is compiled into a
      server side function on every call, which the stream evaluates on each update. Throws
      :type:`~KRPC.Client.FunctionCompilationException` if the lambda cannot be compiled, or makes
      no remote procedure call.

   .. function:: Stream<TResult> AddStream<TResult>(LambdaExpression expression)

      Creates a stream as the overload above does, for a lambda of any delegate type. Throws
      :type:`~KRPC.Client.FunctionCompilationException` for a lambda with parameters, and an
      :type:`System.ArgumentException` if ``TResult`` cannot hold the lambda's value.

   .. function:: Stream<TResult> AddStream<TResult>(KRPC.Client.Services.KRPC.Expression function)

      Creates a stream of the value of a server side function, which takes no arguments. Throws
      an :type:`System.InvalidOperationException` if the function has no value. Throws an
      :type:`System.ArgumentException` if the function takes parameters, or ``TResult`` cannot
      hold its value.

   .. function:: TResult Run<TResult>(Expression<Func<TResult>> expression)

      Runs a lambda expression on the server, within a single physics tick, and returns its
      value. The lambda is compiled into a server side function on every call. Throws
      :type:`~KRPC.Client.FunctionCompilationException` if the lambda cannot be compiled.

   .. function:: void Run(Expression<Action> expression)

      Runs a lambda expression with no value on the server, within a single physics tick. Throws
      :type:`~KRPC.Client.FunctionCompilationException` if the lambda cannot be compiled, or makes
      no remote procedure call.

   .. function:: TResult Run<TResult>(KRPC.Client.Services.KRPC.Expression function, params object[] args)

      Runs a server side function within a single physics tick, and returns its value. The
      arguments are passed to the function's parameters. A null value needs a reference type or a
      nullable value type as ``TResult``.

      Throws an :type:`System.InvalidOperationException` if the function has no value. Throws an
      :type:`System.ArgumentException` if the number of arguments is wrong, a parameter cannot
      take its argument, or ``TResult`` cannot hold the function's value.

   .. function:: void Run(KRPC.Client.Services.KRPC.Expression function, params object[] args)

      Runs a server side function with no value within a single physics tick. The arguments are
      passed to the function's parameters, as for the overload above.

   .. function:: Event AddEvent(Expression<Func<bool>> expression)

      Creates an event from a lambda expression that evaluates to a boolean value, and returns an
      :type:`~KRPC.Client.Event`. The lambda is compiled into a server side function on every
      call. Throws :type:`~KRPC.Client.FunctionCompilationException` if the lambda cannot be
      compiled, or makes no remote procedure call.

   .. function:: Event AddEvent(KRPC.Client.Services.KRPC.Expression function)

      Creates an event from a server side function that takes no arguments and evaluates to a
      boolean value. Throws an :type:`System.ArgumentException` if the function takes parameters,
      or does not evaluate to a boolean value.

   .. function:: KRPC.Client.Services.KRPC.Expression Compile<TResult>(Expression<Func<TResult>> expression)

      Compiles a lambda expression into a server side function. The server compiles the function
      too, so its first run is as fast as later ones. Throws
      :type:`~KRPC.Client.FunctionCompilationException` for a construct with no server side
      equivalent. See :ref:`csharp-client-compiling-lambdas`.

   .. function:: KRPC.Client.Services.KRPC.Expression Compile(Expression<Action> expression)

      Compiles a lambda expression with no value into a server side function.

   .. function:: KRPC.Client.Services.KRPC.Expression Compile<T1,TResult>(Expression<Func<T1,TResult>> expression)

      Compiles a lambda expression with a parameter into a server side function. Overloads take
      up to four parameters, with or without a value.

   .. function:: static KRPC.Schema.KRPC.ProcedureCall GetCall<TResult>(Expression<Func<TResult>> expression)

      Converts a lambda expression that makes one remote procedure call into a
      ``KRPC.Schema.KRPC.ProcedureCall`` message, for use in a server side function built with the
      expression API. See :ref:`server-side-functions-api`. Throws an
      :type:`System.ArgumentException` if the lambda does more than make one call, or calls a
      member that is not a remote procedure.

   .. function:: static KRPC.Schema.KRPC.ProcedureCall GetCall(LambdaExpression expression)

      Converts a lambda expression of any delegate type, as the overload above does.

.. class:: RemoteObject

   The base class of remote objects. Two objects with the same identifier are equal, and hash
   equal.

   .. function:: static ulong GetObjectId(RemoteObject obj)

      The identifier of a remote object on the server, e.g. to pass to
      :meth:`~KRPC.Client.Services.KRPC.Expression.ConstantObject`. Throws an
      :type:`System.ArgumentNullException` if the object is null.

.. class:: Stream<TReturnType>

   A stream. See :ref:`csharp-client-streams`. Two stream objects are equal if they are bound to the
   same stream on the server. Each call to :meth:`~KRPC.Client.Connection.AddStream` creates a new
   stream, so two streams of the same call are not equal.

   .. function:: TReturnType Get()

      Returns the most recent value of the stream, waiting for the first value if none has
      arrived yet. Throws the exception that ended the stream, if it has ended.

   .. function:: bool Wait(double timeout = double.PositiveInfinity)

      Blocks until the value of the stream changes, or the timeout elapses. Returns ``true`` if the
      value changed, and ``false`` on a timeout. Lock :prop:`~KRPC.Client.Stream.Condition` when
      calling this method.

      The timeout is in seconds, and an infinite timeout waits indefinitely. Throws an
      :type:`System.ArgumentOutOfRangeException` for a negative or NaN timeout. Throws the exception
      that ended the stream, if it ends before or during the wait. A thread holding the condition
      sees an ending as an update: the wait returns ``true``, and later reads and waits throw the
      exception.

   .. property:: object Condition { get; }

      A condition variable, locked with the ``lock`` statement, pulsed with ``Monitor.PulseAll``
      when the value of the stream changes.

   .. property:: float Rate { get; set; }

      The update rate of the stream, in Hertz. Zero, the default, means no limit.

   .. function:: int AddCallback(Action<TReturnType> callback)

      Adds a callback, called with each new value of the stream, and returns a tag for
      :meth:`~KRPC.Client.Stream.RemoveCallback`. If the stream already has a value, the callback is
      called with it straight away. The exception that ends a stream goes to its end callbacks.

   .. function:: void RemoveCallback(int tag)

      Removes a callback, given the tag :meth:`~KRPC.Client.Stream.AddCallback` returned.

   .. function:: int AddEndCallback(Action<System.Exception> callback)

      Adds a callback, called once when the stream ends, with the exception that ended it, and
      returns a tag for :meth:`~KRPC.Client.Stream.RemoveEndCallback`. It is called straight away if
      the stream has already ended.

   .. function:: void RemoveEndCallback(int tag)

      Removes an end callback, given the tag :meth:`~KRPC.Client.Stream.AddEndCallback` returned.

   .. function:: void Remove()

      Removes the stream from the server. Other streams of the same call are not affected.

.. class:: Event

   An event, made from a stream of ``bool`` that is true when the event fires. See
   :ref:`csharp-client-events`. Two event objects are equal if they are bound to the same stream
   on the server.

   .. function:: bool Wait(double timeout = double.PositiveInfinity)

      Blocks until the event fires, or the timeout elapses. Returns straight away if the event has
      fired since the previous wait on this event object. Returns ``true`` if the event fired, and
      ``false`` on a timeout. Lock :prop:`~KRPC.Client.Event.Condition` when calling this method.

      The timeout is in seconds, and an infinite timeout waits indefinitely. Throws an
      :type:`System.ArgumentOutOfRangeException` for a negative or NaN timeout. Throws the exception
      that ended the event, if it ends before or during the wait.

   .. property:: object Condition { get; }

      A condition variable, locked with the ``lock`` statement, pulsed with ``Monitor.PulseAll``
      when the event fires.

   .. function:: int AddCallback(Action callback)

      Adds a callback, with no arguments, called each time the event fires, and returns a tag for
      :meth:`~KRPC.Client.Event.RemoveCallback`. It is called straight away for a firing that no
      wait has returned for and no callback has been called for.

   .. function:: void RemoveCallback(int tag)

      Removes a callback, given the tag :meth:`~KRPC.Client.Event.AddCallback` returned.

   .. function:: int AddEndCallback(Action<System.Exception> callback)

      Adds a callback, called once when the event ends, with the exception that ended it, and
      returns a tag for :meth:`~KRPC.Client.Event.RemoveEndCallback`. It is called straight away if
      the event has already ended.

   .. function:: void RemoveEndCallback(int tag)

      Removes an end callback, given the tag :meth:`~KRPC.Client.Event.AddEndCallback` returned.

   .. function:: void Remove()

      Removes the event from the server.

   .. property:: Stream<bool> Stream { get; }

      The stream the event is made from.

.. class:: ConnectionException

   Thrown when connecting fails or the server refuses the connection, and by each call once the
   connection has closed or failed.

   .. property:: KRPC.Schema.KRPC.ConnectionResponse.Types.Status? Status { get; }

      The status the server refused the connection with, such as ``Denied``, or null.

   .. property:: KRPC.Schema.KRPC.Disconnect.Types.Reason? Reason { get; }

      The reason the server gave for ending the connection, such as ``ServerStopped``, or null.

.. class:: RPCException

   Thrown for an error from the server that the client has no exception class for.

.. class:: FunctionCompilationException

   Thrown when a lambda expression cannot be compiled into a server side function. The message
   names the unsupported construct. See :ref:`csharp-client-compiling-lambdas`.

.. class:: IConnection

   The interface :type:`~KRPC.Client.Connection` implements, which the service stubs call.
