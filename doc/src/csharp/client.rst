.. default-domain:: cs
.. highlight:: csharp

C# Client
=========

This client provides a C# API for interacting with a kRPC server. It is distributed as an assembly
named ``KRPC.Client.dll``.

Installing
----------

The C# client is distributed as a NuGet package, available from `nuget.org
<https://www.nuget.org/packages/KRPC.Client>`_ or attached to each `GitHub release
<https://github.com/krpc/krpc/releases>`_. The client requires .NET Framework 4.7.2 or later.

You also need to `install Google.Protobuf using NuGet
<https://www.nuget.org/packages/Google.Protobuf>`_.

.. note::

   The copy of ``Google.Protobuf.dll`` in the GameData folder included with the kRPC server plugin
   should not be used with the client library. It is a modified version to work within KSP.

Getting Started
---------------

To connect to a server, create a :type:`Connection` object. All interaction with the server is done
via this object. When constructed without any arguments, it will connect to the local machine on the
default port numbers. You can specify different connection settings, and also a descriptive name for
the connection, as follows:

.. literalinclude:: /scripts/csharp/client/Connecting.cs

The :type:`Connection` object needs to be disposed of correctly when finished with, so that the
network connection it manages can be released. This can be done with a ``using`` block (as in the
example above) or by calling :meth:`Connection.Dispose` directly.

Calling Remote Procedures
-------------------------

The kRPC server provides *procedures* that a client can run. These procedures are arranged in groups
called *services* to keep things organized. The functionality for the services are defined in the
namespace ``KRPC.Client.Services.*``. For example, all of the functionality provided by the
``SpaceCenter`` service is contained in the namespace ``KRPC.Client.Services.SpaceCenter``.

To interact with a service, you must first instantiate it. You can then call its methods and
properties to invoke remote procedures. The following example demonstrates how to do this. It
instantiates the ``SpaceCenter`` service and calls
:prop:`KRPC.Client.Services.SpaceCenter.SpaceCenter.ActiveVessel` to get an object representing the
active vessel (of type :type:`KRPC.Client.Services.SpaceCenter.Vessel`). It sets the name of the
vessel and then prints out its altitude:

.. literalinclude:: /scripts/csharp/client/RemoteProcedures.cs

Many procedures return an object identifying something in the game, such as a vessel or
a part. See :doc:`Object Lifetime </tutorials/object-lifetime>` for how such an object
behaves when a game is loaded or what it identifies is destroyed.

Server Side Functions
---------------------

A server side function is code that runs inside the game, on the server. Pass a lambda expression to
:meth:`Connection.Run` to compile it into a server side function, run it once and return its value.
Every value it reads comes from the same physics tick. For example, the following reads the vessel's
altitude and speed together:

.. literalinclude:: /scripts/csharp/client/RunFunction.cs

Use ``Run`` for a function with effects, such as a call of ``ActivateNextStage``. An event or a
function stream evaluates its function on every update, repeating the effects.

:ref:`csharp-client-compiling-lambdas` describes what a function can contain. The :ref:`server side
functions tutorial <tutorial-server-side-functions>` shows how to build larger functions.

.. _csharp-client-streams:

Streaming Data
--------------

A common use case for kRPC is to continuously extract data from the game. The naive approach to do
this would be to repeatedly call a remote procedure, such as in the following which repeatedly
prints the position of the active vessel:

.. literalinclude:: /scripts/csharp/client/Streaming1.cs

This approach requires significant communication overhead as request/response messages are
repeatedly sent between the client and server. kRPC provides a more efficient mechanism to achieve
this, called *streams*.

A stream repeatedly executes a procedure on the server (with a fixed set of argument values) and
sends the result to the client. It only requires a single message to be sent to the server to
establish the stream, which will then continuously send data to the client until the stream is
closed.

The following example does the same thing as above using streams:

.. literalinclude:: /scripts/csharp/client/Streaming2.cs

It calls :meth:`Connection.AddStream` once at the start of the program to create the stream, and
then repeatedly prints the position returned by the stream. The stream is automatically closed when
the client disconnects.

To stream a method call, pass :meth:`Connection.AddStream` a lambda expression with no parameters
that makes the call. A single call with fixed arguments is streamed as that call. The instance it
is made on is evaluated once, when the stream is created. For example,
``vessel.Flight(frame).MeanAltitude`` streams the ``MeanAltitude`` call.

Any other lambda is compiled into a server side function, as
:ref:`csharp-client-compiling-lambdas` describes. A call with a remote procedure call in an
argument is one of these, and the argument's call is made on each update.

:meth:`Connection.AddStream` returns a stream object of type :type:`Stream`. The most recent
value of the stream can be obtained by calling :meth:`Stream.Get`. A stream can be stopped and
removed from the server by calling :meth:`Stream.Remove` on the stream object. All of a client's
streams are automatically stopped when it disconnects.

.. note::

   A stream runs on the server until you remove it or the connection closes, even once the stream
   object is garbage collected. Each call to ``AddStream`` adds another stream.

Synchronizing with Stream Updates
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

A common use case for kRPC is to wait until the value returned by a method or attribute changes, and
then take some action. kRPC provides two mechanisms to do this efficiently: *condition variables*
and *callbacks*.

Condition Variables
~~~~~~~~~~~~~~~~~~~

Each stream has a condition variable associated with it, that is notified whenever the value of the
stream changes. These can be used to block the current thread of execution until the value of the
stream changes.

The following example waits until the abort button is pressed in game, by waiting for the value of
:prop:`KRPC.Client.Services.SpaceCenter.Control.Abort` to change to true:

.. literalinclude:: /scripts/csharp/client/ConditionVariables.cs

This code creates a stream, acquires a lock on the streams condition variable (by using a ``lock``
statement) and then repeatedly checks the value of ``Abort``. It leaves the loop when it changes to
true.

The body of the loop calls ``Wait`` on the stream, which causes the program to block until the value
changes. This prevents the loop from 'spinning' and so it does not consume processing resources
whilst waiting.

Callbacks
~~~~~~~~~

Streams allow you to register callback functions that are called whenever the value of the stream
changes. Callback functions should take a single argument, which is the new value of the stream, and
should return nothing.

For example the following program registers two callbacks that are invoked when the value of
:prop:`KRPC.Client.Services.SpaceCenter.Control.Abort` changes:

.. literalinclude:: /scripts/csharp/client/Callbacks.cs

A callback added to a stream that already has a value is called with that value straight away.

.. note::

   The callback function may be called from a different thread than the one that created the
   stream. Any changes to shared state must therefore be protected with appropriate
   synchronization.

Stream Endings
^^^^^^^^^^^^^^

The following example streams the temperature of a part until the part is destroyed:

.. literalinclude:: /scripts/csharp/client/StreamEnd.cs

Once a stream ends, calling ``Get`` or ``Wait`` on the stream throws the exception that ended it. A
stream ends when its procedure throws an exception, when the server removes it, when you remove it,
or when the connection closes.

A stream that the server removes, such as an event that has finished, ends with
``KRPC.Client.Services.KRPC.StreamRemovedException``. A stream you remove ends with an
``InvalidOperationException``, and a closed connection with a ``ConnectionException``.

An exception for an object that is not loaded, or a procedure not available in the game scene,
holds the stream instead. It resumes once the object or scene is available.

.. seealso:: :doc:`/tutorials/object-lifetime`

To handle an ending in a callback, add an end callback with :meth:`Stream.AddEndCallback`. It is
called once, with the exception.

Streaming Server Side Functions
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Pass a lambda expression to :meth:`Connection.AddStream` to stream its value, computed on the
server. The server evaluates the function on each stream update, within one physics tick. The value
can be any type the server can send, including collections and objects. For example, the following
streams the vessel's altitude in kilometers:

.. literalinclude:: /scripts/csharp/client/CompiledFunctionStream.cs

Given a function built with the expression API, :meth:`Connection.AddStream` takes the type of
its value as a type parameter. The type must be able to hold the value, or ``AddStream`` throws an
``InvalidOperationException``.

A function stream or event ends at the first exception its function throws. This includes the
``KRPC.ObjectNotLoadedException`` of a call on a part of an unloaded vessel, or on a vessel during
a game load. To keep the function running, catch the exception with ``Expression.TryCatch``. The
following event does not fire while the part is not loaded:

.. code-block:: csharp

   var hot = Expression.Variable(connection, "hot", Type.Bool(connection));
   var function = Expression.BlockWithVariables(connection,
       new List<Expression> { hot },
       new List<Expression> {
           Expression.TryCatch(connection,
               Expression.Assign(connection, hot,
                   connection.Compile(() => part.Temperature > 1000)),
               "KRPC", "ObjectNotLoadedException", null,
               Expression.Assign(connection, hot, Expression.ConstantBool(connection, false))),
           hot
       });
   var evnt = connection.AddEvent(function);

.. _csharp-client-events:

Events
------

Some procedures return event objects of type :type:`Event`. These allow you to wait until an event
occurs, by calling :meth:`Event.Wait`. Under the hood, these are implemented using streams and
condition variables.

A call to ``Wait`` returns straight away if the event has fired since the previous call on the same
event object, so no firing is missed between two calls. Several firings between two calls make
``Wait`` return once.

You can also create a custom event from a lambda expression. The following creates an event that
triggers when ``MeanAltitude > 1000``:

.. literalinclude:: /scripts/csharp/client/CompiledEvent.cs

You can also build the event's function with the :ref:`expression API <server-side-functions-api>`,
and pass it to :meth:`Connection.AddEvent`. :meth:`Connection.GetCall` gives the call of a
procedure, for ``Expression.Call``. The following builds the same event:

.. literalinclude:: /scripts/csharp/client/Event.cs

.. _csharp-client-compiling-lambdas:

Compiling Server Side Functions
-------------------------------

Pass a lambda expression to :meth:`Connection.Compile` to compile it into a server side function.
:meth:`Connection.AddEvent`, :meth:`Connection.AddStream` and :meth:`Connection.Run` take the
result, and run it without compiling it again. The following compiles a function once, and runs it
every second:

.. literalinclude:: /scripts/csharp/client/CompiledFunction.cs

Given a lambda, ``AddEvent`` and ``Run`` translate it on every call, and so does ``AddStream``
unless it streams a single call. The lambda's captured variables take their current values. A
compiled function keeps the values from when it was compiled.

Compiling makes a remote procedure call per node. To use a lambda repeatedly, compile it once with
``Compile`` and pass the result. ``Compile`` also compiles the function on the server, so its first
run is as fast as later ones.

A lambda can take up to four parameters. ``Run`` passes its arguments to them:

.. code-block:: csharp

   var flight = connection.SpaceCenter().ActiveVessel.Flight();
   var altitudeAbove = connection.Compile((double height) => flight.MeanAltitude - height);
   Console.WriteLine(connection.Run<double>(altitudeAbove, 70000));

An argument is encoded at its parameter's type, and a number widens as C# widens it. A signed
integer that is not negative converts to an unsigned parameter at least as wide, so a ``long``
converts to a ``ulong`` but not a ``uint``.

An array other than an ``object[]``, passed as the only argument, is the argument of a function
with one parameter. An event or a function stream takes a function with no parameters.

The lambda is a C# expression tree, so C#'s expression tree rules apply. It holds a single
expression, and a call in it gives every argument, including the optional ones. Build a function
with statements, or one that raises or handles exceptions, with the
:ref:`expression API <server-side-functions-api>`.

The compiler translates the lambda's expression tree into a server side function that computes
the same result on the server:

* Property accesses and method calls on remote objects and services become calls in the
  function, made again on each evaluation. This includes calls on the elements of a collection
  in a LINQ operator. The procedures of the ``KRPC`` service cannot be called.
* Captured variables, literals and any sub-expression that makes no remote call are evaluated
  once, when the function is compiled, and embedded as constants. A captured value in a string
  concatenation is formatted on the client, as C# formats it.
* A delegate call is an error. So is a call to a method, a constructor, a computed property or an
  operator defined outside the .NET base library and the kRPC client. Call it outside the lambda
  and use its result. An auto-property is read once, when compiling, as a captured variable is.
* Passing a delegate, or a value whose type overrides a method such as ``ToString``, to the base
  library is an error, as in ``"a" + obj``, ``list.Select(MyMethod)`` and your own
  ``IEnumerable<T>`` or ``IComparer<T>`` given to LINQ. So is reading ``Lazy<T>.Value``.
* Operators, conditional expressions, casts and indexing compile to the matching expression
  operators. So do ``List``, ``HashSet`` and ``Dictionary`` initializers, ``Tuple.Create``, and
  a tuple's ``Item1`` to ``Item7``. A dictionary's ``Keys`` and ``Values`` are lists of its keys
  and values.
* ``&&`` and ``||`` short-circuit on the server. ``&`` and ``|`` evaluate both operands.
* Comparing a value with ``null`` tests whether it is null. ``??``, ``HasValue``, ``Value`` and
  ``GetValueOrDefault`` behave as in C#.
* ``==`` and ``!=`` compare tuples and collections by their contents. C# compares them by
  reference.
* An ordering comparison with a nullable number is false when the number is null. Arithmetic
  and logic on a null throw.
* The ``System.Math`` methods ``Abs``, ``Sign``, ``Min``, ``Max``, ``Clamp``, ``Floor``,
  ``Ceiling``, ``Round``, ``Pow``, ``Sqrt``, ``Exp``, ``Log``, ``Log10``, ``Sin``, ``Cos``,
  ``Tan``, ``Asin``, ``Acos``, ``Atan`` and ``Atan2`` take server side arguments. Integer
  ``Abs``, ``Min``, ``Max`` and ``Clamp`` are exact at their own type. ``Math.Round`` takes the
  ``MidpointRounding.ToEven`` and ``AwayFromZero`` modes, given as a client side value. Unlike
  C#, it takes any number of decimal places, and a negative number rounds to tens, hundreds and
  beyond.
* ``Console.WriteLine`` writes a line to the game's log with ``StdLib.Print``. It formats its
  arguments as ``string.Format`` does. Convert a number computed on the server with
  ``ToString(CultureInfo.InvariantCulture)`` before passing it.
* The string members ``Length``, ``Substring``, ``IndexOf``, ``Contains``, ``StartsWith``,
  ``EndsWith``, ``Replace``, ``ToUpperInvariant``, ``ToLowerInvariant``, ``Trim``,
  ``TrimStart`` and ``TrimEnd`` compile to the server's string operations. So does
  ``string.Join``.
* An interpolated string, ``string.Concat``, and ``string.Format`` with plain ``{0}``
  placeholders compile to a concatenation.
* ``ToString`` of an object computed on the server gives the server's name for its class, which
  differs from the client's.
* A ``double`` converted to a string on the server is formatted by the server's runtime. It can
  print fewer digits than the client, as in ``0.3`` for ``0.30000000000000004``.
* An index out of range in ``list[i]``, ``ElementAt`` or ``Substring`` throws
  ``IndexOutOfRangeException``, where C# throws ``ArgumentOutOfRangeException``.
* The LINQ operators ``Select``, ``Where``, ``SelectMany``, ``Any`` and ``All`` with a
  predicate, ``Count``, ``Sum``, ``Min``, ``Max`` and ``Average`` without one, ``Contains``,
  ``OrderBy``, ``Concat``, ``Skip``, ``Take``, ``First``, ``Last``, ``ElementAt``,
  ``Distinct``, ``Reverse``, ``Union``, ``Intersect``, ``Except``, ``Zip``, ``ToDictionary``,
  ``ToList`` and ``ToHashSet`` compile to the server's collection operations. A key repeated
  in ``ToDictionary`` takes the last of its values, where C# throws.
* A lambda that evaluates to an ``IEnumerable<T>`` evaluates to a list. One that evaluates to
  the ``IOrderedEnumerable<T>`` of ``OrderBy`` is an error. Call ``ToList`` on it.
* Reading a field of a structure a service defines reads that field of it on the server.
  Constructing one with ``new`` builds it there. Comparing two with ``<``, ``>``, ``<=`` or
  ``>=`` is an error.
* Reading a stream with ``Stream.Get`` is an error. Make the stream's remote procedure call in
  the lambda instead.

The server formats and compares strings with the invariant culture. A call that depends on the
current culture is an error:

* A number converts to a string with ``ToString(CultureInfo.InvariantCulture)``. Format an
  interpolated string with ``FormattableString.Invariant``, or call ``string.Format`` with
  ``CultureInfo.InvariantCulture``.
* ``IndexOf``, ``StartsWith`` and ``EndsWith`` with a string argument take
  ``StringComparison.Ordinal``.
* ``ToUpper`` and ``ToLower`` take ``CultureInfo.InvariantCulture``.
* ``OrderBy`` with a string key takes ``StringComparer.Ordinal``.

Integer arithmetic wraps around on overflow, so ``checked`` arithmetic is an error.

A construct with no server side equivalent throws :type:`FunctionCompilationException` naming it.
Examples are ``GroupBy``, ``Aggregate``, ``MinBy``, ``MaxBy`` and a format specifier.

A lambda that makes no remote procedure call is an error in an event or a stream, as its value is
the same on every update. An ``Action`` lambda that makes no remote procedure call is an error in
``Compile`` and ``Run``, as it has no effect on the server.

Client API Reference
--------------------

.. class:: IConnection

   Interface implemented by the :type:`Connection` class.

.. class:: Connection

   A connection to the kRPC server. All interaction with kRPC is performed via an instance of this
   class.

   .. function:: Connection(string name = "", System.Net.IPAddress address = null, int rpcPort = 50000, int streamPort = 50001, System.TimeSpan timeout = default)

      Connect to a kRPC server.

      :parameters:

       * **name** -- A descriptive name for the connection. This is passed to the server and appears
         in the in-game server window.
       * **address** -- The address of the server to connect to. Defaults to 127.0.0.1.
       * **rpc_port** -- The port number of the RPC Server. Defaults to 50000. This should match the
         RPC port number of the server you want to connect to.
       * **stream_port** -- The port number of the Stream Server. Defaults to 50001. This should
         match the stream port number of the server you want to connect to.
       * **timeout** -- How long to wait for a connection before giving up. Defaults to zero,
         which waits indefinitely. A network that drops a connection attempt rather than refusing
         it otherwise leaves the client waiting.

   .. function:: static Connection ConnectLocal(string name = "", string rpcPath = "", string streamPath = "")

      Connect to a kRPC server running on the same machine, over unix domain sockets rather than
      TCP/IP. The connection behaves identically once established. Unix domain sockets are
      available on Linux, macOS, and Windows 10 1803 and Windows Server 2019 or later.

      :parameters:

       * **name** -- A descriptive name for the connection. This is passed to the server and appears
         in the in-game server window.
       * **rpcPath** -- The path of the socket the RPC Server is listening on. This should match the
         RPC socket path shown in the in-game server window. Leave empty for the path the server
         uses unless it was configured with another.
       * **streamPath** -- The path of the socket the Stream Server is listening on. This should
         match the stream socket path shown in the in-game server window. Defaults as ``rpcPath``
         does. Pass ``null`` to connect without stream support.

   .. function:: Stream<ReturnType> AddStream<ReturnType>(LambdaExpression expression)

      Create a stream from a lambda expression. A single remote procedure call with fixed
      arguments is streamed as that call. A cast of a call to a ``KRPC`` service procedure is
      too, with the cast made on the client. Any other lambda is translated as
      :meth:`Connection.Compile` translates it, on every call. Throws an ``ArgumentException`` if
      ``ReturnType`` cannot hold the lambda's value, and :type:`FunctionCompilationException` if
      the lambda cannot be compiled.

   .. function:: Stream<ReturnType> AddStream<ReturnType>(KRPC.Client.Services.KRPC.Expression function)

      Create a stream of the value of a server side function. Throws an
      ``InvalidOperationException`` if the function has no value or the type parameter cannot
      hold its value.

   .. function:: KRPC.Client.Services.KRPC.Expression Compile<ReturnType>(Expression<Func<ReturnType>> expression)

      Compile a lambda expression into a server side function. Throws
      :type:`FunctionCompilationException` if the lambda cannot run on the server. Compiling
      makes a remote procedure call per node, so compile a function once and reuse it. The
      server compiles the function too, so its first run is as fast as later ones.

   .. function:: KRPC.Client.Services.KRPC.Expression Compile(Expression<Action> expression)

      Compile a lambda expression with no value into a server side function.

   .. function:: KRPC.Client.Services.KRPC.Expression Compile<T1,ReturnType>(Expression<Func<T1,ReturnType>> expression)

      Compile a lambda expression with a parameter into a server side function. Overloads take
      up to four parameters, and a lambda with no value.

   .. function:: Event AddEvent(Expression<Func<bool>> expression)

      Create an event from a lambda expression that evaluates to a boolean value. The lambda is
      translated as :meth:`Connection.Compile` translates it, on every call.

   .. function:: Event AddEvent(KRPC.Client.Services.KRPC.Expression function)

      Create an event from a server side function that evaluates to a boolean value.

   .. function:: ReturnType Run<ReturnType>(KRPC.Client.Services.KRPC.Expression function, params object[] args)

      Run a server side function within a single physics tick, and return its value. The
      arguments are passed to the function's parameters. Throws an
      ``InvalidOperationException`` if the function has no value or the type parameter cannot hold
      its value, and an ``ArgumentException`` if the number of arguments is wrong or a parameter
      cannot take its argument. A null result needs a reference type or a nullable value type as
      the type parameter.

   .. function:: ReturnType Run<ReturnType>(Expression<Func<ReturnType>> expression)

      Run a lambda expression on the server within a single physics tick, and return its value.
      The lambda is translated as :meth:`Connection.Compile` translates it, on every call.

   .. function:: void Run(KRPC.Client.Services.KRPC.Expression function, params object[] args)

      Run a server side function with no value within a single physics tick. The arguments are
      passed to the function's parameters.

   .. function:: void Run(Expression<Action> expression)

      Run a lambda expression with no value on the server within a single physics tick.

   .. function:: KRPC.Schema.KRPC.ProcedureCall GetCall(LambdaExpression expression)

      Returns the procedure call message for a lambda expression that makes a single call. The
      expression API's ``Expression.Call`` takes it, as in :ref:`csharp-client-events`.

   .. function:: void Dispose()

      Closes the connection and frees the resources associated with it.

.. class:: RemoteObject

   The base class for remote objects.

   .. function:: static ulong GetObjectId(RemoteObject obj)

      The identifier of a remote object on the server, e.g. to pass to
      :meth:`KRPC.Client.Services.KRPC.Expression.ConstantObject`. Throws an
      ``ArgumentNullException`` if the object is null.

.. class:: FunctionCompilationException

   Thrown when a lambda expression cannot be compiled into a server side function.

.. class:: Stream<ReturnType>

   This class represents a stream. See :ref:`csharp-client-streams`.

   Stream objects implement ``GetHashCode``, ``Equals``, ``operator ==`` and ``operator !=`` such
   that two stream objects are equal if they are bound to the same stream on the server. Each call
   to :meth:`Connection.AddStream` creates a new stream, so two streams of the same call are not
   equal.

   .. property:: float Rate { get; set; }

      The update rate of the stream in Hertz. When set to zero, the rate is unlimited.

   .. function:: ReturnType Get()

      Returns the most recent value for the stream, waiting for the first value if none has arrived
      yet. Throws the exception that ended the stream, if it has ended, such as an exception thrown
      by the remote procedure.

      Throws an ``InvalidOperationException`` if the value is a null that ``ReturnType``
      cannot hold.

   .. property:: object Condition { get; }

      A condition variable that is notified (using ``Monitor.PulseAll``) whenever the value of the
      stream changes.

   .. function:: bool Wait(double timeout = -1)

      This method blocks until the value of the stream changes or the operation times out. Returns
      ``true`` if the value changed, and ``false`` on a timeout.

      The streams condition variable must be locked before calling this method.

      If *timeout* is specified and is greater than or equal to 0, it is the timeout in seconds for
      the operation. Throws the exception that ended the stream, if it ends before or during the
      wait.

   .. function:: int AddCallback(Action<ReturnType> callback)

      Adds a callback function that is invoked whenever the value of the stream changes. The
      callback function should take one argument, which is passed the new value of the
      stream. If the stream already has a value, the callback is invoked with it straight away.
      An error ends the stream and does not invoke the callback. Calling ``Get()`` throws it.
      Returns a unique identifier for the callback which can be used to remove it.

      .. note::

         The callback function may be called from a different thread than the one that created the
         stream. Any changes to shared state must therefore be protected with appropriate
         synchronization.

   .. function:: void RemoveCallback(int tag)

      Removes a callback from the stream. The tag is the identifier returned when the callback was
      added.

   .. function:: int AddEndCallback(Action<System.Exception> callback)

      Adds a callback function that is invoked once when the stream ends. The callback function
      should take one argument, which is passed the exception that ended the stream. It is invoked
      straight away if the stream has already ended. Returns a unique identifier for the callback
      which can be used to remove it.

   .. function:: void RemoveEndCallback(int tag)

      Removes an end callback from the stream. The tag is the identifier returned when the callback
      was added.

   .. function:: void Remove()

      Removes the stream from the server. Other streams of the same call are not affected.

.. class:: Event

   This class represents an event. See :ref:`csharp-client-events`. It is wrapper around a
   :type:`Stream<bool>` that indicates when the event occurs.

   Event objects implement ``GetHashCode``, ``Equals``, ``operator ==`` and ``operator !=`` such
   that two event objects are equal if they are bound to the same underlying stream on the server.

   .. property:: object Condition { get; }

      The condition variable that is notified (using ``Monitor.PulseAll``) whenever the event
      occurs.

   .. function:: bool Wait(double timeout = -1)

      This method blocks until the event occurs or the operation times out. It returns straight
      away if the event has occurred since the previous call on this event object. Returns ``true``
      if the event occurred, and ``false`` on a timeout.

      The events condition variable must be locked before calling this method.

      If *timeout* is specified and is greater than or equal to 0, it is the timeout in seconds for
      the operation. Throws the exception that ended the event, if it ends before or during the
      wait.

   .. function:: int AddCallback(Action callback)

      Adds a callback function that is invoked whenever the event occurs. The callback function
      should be a function that takes zero arguments. If the event has not ended and has fired
      since the last ``Wait`` on this event object returned and since its callbacks were last
      invoked, the callback is invoked once straight away. An error ends the event and does not
      invoke the callback. Returns a unique identifier for the callback which can be used to
      remove it.

   .. function:: void RemoveCallback(int tag)

      Removes a callback from the event. The tag is the identifier returned when the callback was
      added.

   .. function:: int AddEndCallback(Action<System.Exception> callback)

      Adds a callback function that is invoked once when the event ends, with the exception that
      ended it. It is invoked straight away if the event has already ended. Returns a unique
      identifier for the callback which can be used to remove it.

   .. function:: void RemoveEndCallback(int tag)

      Removes an end callback from the event. The tag is the identifier returned when the callback
      was added.

   .. function:: void Remove()

      Removes the event from the server.

   .. property:: Stream<bool> Stream { get; }

      Returns the underlying stream for the event.
