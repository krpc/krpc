.. default-domain:: csharp
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

.. literalinclude:: /scripts/client/csharp/Connecting.cs

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

.. literalinclude:: /scripts/client/csharp/RemoteProcedures.cs

Many procedures return an object standing for something in the game, such as a vessel or
a part. See :doc:`Object Lifetime </tutorials/object-lifetime>` for what those objects do
when a game is loaded or the thing they stand for is destroyed.

.. _csharp-client-streams:

Streaming Data
--------------

A common use case for kRPC is to continuously extract data from the game. The naive approach to do
this would be to repeatedly call a remote procedure, such as in the following which repeatedly
prints the position of the active vessel:

.. literalinclude:: /scripts/client/csharp/Streaming1.cs

This approach requires significant communication overhead as request/response messages are
repeatedly sent between the client and server. kRPC provides a more efficient mechanism to achieve
this, called *streams*.

A stream repeatedly executes a procedure on the server (with a fixed set of argument values) and
sends the result to the client. It only requires a single message to be sent to the server to
establish the stream, which will then continuously send data to the client until the stream is
closed.

The following example does the same thing as above using streams:

.. literalinclude:: /scripts/client/csharp/Streaming2.cs

It calls :meth:`Connection.AddStream` once at the start of the program to create the stream, and
then repeatedly prints the position returned by the stream. The stream is automatically closed when
the client disconnects.

A stream can be created for any method call by calling :meth:`Connection.AddStream` and passing it
a lambda expression, taking zero arguments, that invokes the desired method. A lambda that is
anything more than a single method call or property access is compiled into a server side
expression, as described in `Compiling Lambdas to Expressions`_. It returns a stream object of
type :type:`Stream`. The most recent value of the stream can be obtained by calling
:meth:`Stream.Get`. A stream can be stopped and removed from the server by calling
:meth:`Stream.Remove` on the stream object. All of a clients streams are automatically stopped when
it disconnects.

Synchronizing with Stream Updates
---------------------------------

A common use case for kRPC is to wait until the value returned by a method or attribute changes, and
then take some action. kRPC provides two mechanisms to do this efficiently: *condition variables*
and *callbacks*.

Condition Variables
^^^^^^^^^^^^^^^^^^^

Each stream has a condition variable associated with it, that is notified whenever the value of the
stream changes. These can be used to block the current thread of execution until the value of the
stream changes.

The following example waits until the abort button is pressed in game, by waiting for the value of
:prop:`KRPC.Client.Services.SpaceCenter.Control.Abort` to change to true:

.. literalinclude:: /scripts/client/csharp/ConditionVariables.cs

This code creates a stream, acquires a lock on the streams condition variable (by using a ``lock``
statement) and then repeatedly checks the value of ``Abort``. It leaves the loop when it changes to
true.

The body of the loop calls ``Wait`` on the stream, which causes the program to block until the value
changes. This prevents the loop from 'spinning' and so it does not consume processing resources
whilst waiting.

.. note::

   The stream does not start receiving updates until the first call to ``Wait``. This means that the
   example code will not miss any updates to the streams value, as it will have already locked the
   condition variable before the first stream update is received.

Callbacks
^^^^^^^^^

Streams allow you to register callback functions that are called whenever the value of the stream
changes. Callback functions should take a single argument, which is the new value of the stream, and
should return nothing.

For example the following program registers two callbacks that are invoked when the value of
:prop:`KRPC.Client.Services.SpaceCenter.Control.Abort` changes:

.. literalinclude:: /scripts/client/csharp/Callbacks.cs

.. note::

   When a stream is created it does not start receiving updates until ``Start`` is called. This is
   implicitly called when accessing the value of a stream, but as this example does not do this an
   explicit call to ``Start`` is required.

.. note::

   The callbacks are registered before the call to ``Start`` so that stream updates are not missed.

.. note::

   The callback function may be called from a different thread to that which created the stream. Any
   changes to shared state must therefore be protected with appropriate synchronization.

.. _csharp-client-events:

Custom Events
-------------

Some procedures return event objects of type :type:`Event`. These allow you to wait until an event
occurs, by calling :meth:`Event.Wait`. Under the hood, these are implemented using streams and
condition variables.

Custom events can also be created. An expression API allows you to create code that runs on the
server and these can be used to build a custom event. For example, the following creates the
expression ``MeanAltitude > 1000`` and then creates an event that will be triggered when the
expression returns true:

.. literalinclude:: /scripts/client/csharp/Event.cs

Expression Streams
------------------

Expressions can also be used to stream the result of a computation from the server, by calling
:meth:`Connection.AddStream` with an expression object. Values are computed on the server on
each stream update, so complex telemetry can be received without the round trip latency of
multiple RPCs, and without the values changing between calls. The expression can evaluate to
any type that can be sent to a client, including collections and objects; the type parameter
must correspond to the expression's return type. For example, the following streams the
vessel's altitude, converted to kilometers on the server:

.. literalinclude:: /scripts/client/csharp/ExpressionStream.cs

Compiling Lambdas to Expressions
--------------------------------

Instead of building an expression from the factory methods directly, a lambda expression that
takes no arguments can be compiled into a server side expression using
:meth:`Connection.CompileExpression`. :meth:`Connection.AddEvent` accepts a boolean lambda
directly, and :meth:`Connection.AddStream` compiles any lambda that is not a single method call
or property access:

.. literalinclude:: /scripts/client/csharp/CompiledExpression.cs

The compiler translates the lambda's expression tree into an expression that computes the same
result on the server:

* Remote procedure calls made by the lambda — property accesses and method calls on remote
  objects and services — become calls embedded in the expression, re-invoked on each
  evaluation. This includes calls on the elements of collections inside LINQ operators.
* All other values — captured variables, literals, and any sub-expression that does not involve
  the server — are evaluated once, when the expression is compiled, and embedded as constants.
* Arithmetic, comparison and bitwise operators, boolean operators (note that ``&&`` and
  ``||`` do not short-circuit when evaluated on the server), conditional expressions, casts,
  string concatenation and ``ToString``, tuple and collection constructors, and indexing are
  translated to the corresponding expression operators. Calls to ``System.Math`` methods with
  server side arguments are compiled to the ``StdLib`` service's procedures.
* The LINQ operators ``Select``, ``Where``, ``SelectMany``, ``Any``, ``All``, ``Count``,
  ``Sum``, ``Min``, ``Max``, ``Average``, ``Contains``, ``OrderBy``, ``Concat``, ``Skip``,
  ``Take``, ``ToDictionary``, ``ToList`` and ``ToHashSet`` are translated to the server's
  collection operations. A lazily evaluated sequence produced at the top level of the lambda
  is implicitly converted to a list.
* Reading a field of a structure a service defines reads that field of it on the server, and
  constructing one with ``new`` builds it there.

Anything else cannot run on the server, and throws :type:`ExpressionCompilationException`
describing the unsupported construct.

Client API Reference
--------------------

.. class:: IConnection

   Interface implemented by the :type:`Connection` class.

.. class:: Connection

   A connection to the kRPC server. All interaction with kRPC is performed via an instance of this
   class.

   .. method:: Connection(string name = "", System.Net.IPAddress address = null, int rpcPort = 50000, int streamPort = 50001, System.TimeSpan timeout = default)

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

   .. method:: static Connection ConnectLocal(string name = "", string rpcPath = "", string streamPath = "")

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

   .. method:: Stream<ReturnType> AddStream<ReturnType>(LambdaExpression expression)

      Create a new stream from the given lambda expression. A lambda consisting of a single
      method call or property access is streamed as that remote procedure call. Any other
      lambda is compiled into a server side expression using
      :meth:`Connection.CompileExpression`, evaluated on the server on each stream update.

   .. method:: KRPC.Client.Services.KRPC.Expression CompileExpression<ReturnType>(Expression<Func<ReturnType>> expression)

      Compile a lambda expression, taking no arguments, into a server side expression that
      computes the same result on the server. Remote procedure calls made by the lambda are
      re-invoked on each evaluation of the expression; other values are captured when the
      expression is compiled. Throws :type:`ExpressionCompilationException` for constructs
      that cannot run on the server.

   .. method:: Event AddEvent(Expression<Func<bool>> expression)

      Create an event from a boolean lambda expression, compiled into a server side expression
      using :meth:`Connection.CompileExpression`.

   .. method:: ReturnType RunFunction<ReturnType>(Expression<Func<ReturnType>> expression)

      Run a function on the server, within a single physics tick, and return the value it
      produces. The lambda expression is compiled using :meth:`Connection.CompileExpression`.
      Also callable with a server side expression object in place of the lambda. This is the
      intended way to use functions with side effects, which would otherwise re-run on every
      update of an event or stream.

   .. method:: void RunFunction(Expression<Action> expression)

      Run a function with no result on the server, within a single physics tick, for its
      effects — for example a lambda that invokes a single remote method. Also callable with
      a server side expression object, which may be a statement block built with the
      expression API.

   .. method:: KRPC.Schema.KRPC.ProcedureCall GetCall(LambdaExpression expression)

      Returns a procedure call message for the given lambda expression. This allows descriptions of
      procedure calls to be passed to the server, for example when constructing custom events. See
      :ref:`csharp-client-events`.

   .. method:: void Dispose()

      Closes the connection and frees the resources associated with it.

.. class:: ExpressionCompilationException

   Thrown when a lambda expression cannot be compiled into a server side expression.

.. class:: Stream<ReturnType>

   This class represents a stream. See :ref:`csharp-client-streams`.

   Stream objects implement ``GetHashCode``, ``Equals``, ``operator ==`` and ``operator !=`` such
   that two stream objects are equal if they are bound to the same stream on the server.

   .. method:: void Start(bool wait = true)

      Starts the stream. When a stream is created by calling :meth:`Connection.AddStream` it does
      not start sending updates to the client until this method is called.

      If wait is true, this method will block until at least one update has been received from the
      server.

      If wait is false, the method starts the stream and returns immediately. Subsequent calls to
      :meth:`Get` may raise an ``InvalidOperationException`` if the stream does not yet contain a
      value.

   .. property:: float Rate { get; set; }

      The update rate of the stream in Hertz. When set to zero, the rate is unlimited.

   .. method:: ReturnType Get()

      Returns the most recent value for the stream. If executing the remote procedure for the stream
      throws an exception, calling this method will rethrow the exception. Raises an
      ``InvalidOperationException`` if no update has been received from the server.

      If the stream has not been started this method calls ``Start(true)`` to start the stream and
      wait until at least one update has been received.

   .. property:: object Condition { get; }

      A condition variable that is notified (using ``Monitor.PulseAll``) whenever the value of the
      stream changes.

   .. method:: void Wait(double timeout = -1)

      This method blocks until the value of the stream changes or the operation times out.

      The streams condition variable must be locked before calling this method.

      If *timeout* is specified and is greater than or equal to 0, it is the timeout in seconds for
      the operation.

      If the stream has not been started this method calls ``Start(false)`` to start the stream
      (without waiting for at least one update to be received).

   .. method:: int AddCallback(Action<ReturnType> callback)

      Adds a callback function that is invoked whenever the value of the stream changes. The
      callback function should take one argument, which is passed the new value of the
      stream. Returns a unique identifier for the callback which can be used to remove it.

      .. note::

         The callback function may be called from a different thread to that which created the
         stream. Any changes to shared state must therefore be protected with appropriate
         synchronization.

   .. method:: void RemoveCallback(int tag)

      Removes a callback from the stream. The tag is the identifier returned when the callback was
      added.

   .. method:: void Remove()

      Removes the stream from the server.

.. class:: Event

   This class represents an event. See :ref:`csharp-client-events`. It is wrapper around a
   :type:`Stream<bool>` that indicates when the event occurs.

   Event objects implement ``GetHashCode``, ``Equals``, ``operator ==`` and ``operator !=`` such
   that two event objects are equal if they are bound to the same underlying stream on the server.

   .. method:: void Start()

      Starts the event. When an event is created, it will not receive updates from the server until
      this method is called.

   .. property:: object Condition { get; }

      The condition variable that is notified (using ``Monitor.PulseAll``) whenever the event
      occurs.

   .. method:: void Wait(double timeout = -1)

      This method blocks until the event occurs or the operation times out.

      The events condition variable must be locked before calling this method.

      If *timeout* is specified and is greater than or equal to 0, it is the timeout in seconds for
      the operation.

      If the event has not been started this method calls ``Start()`` to start the underlying
      stream.

   .. method:: int AddCallback(Action callback)

      Adds a callback function that is invoked whenever the event occurs. The callback function
      should be a function that takes zero arguments. Returns a unique identifier for the callback
      which can be used to remove it.

   .. method:: void RemoveCallback(int tag)

      Removes a callback from the event. The tag is the identifier returned when the callback was
      added.

   .. method:: void Remove()

      Removes the event from the server.

   .. property:: Stream<bool> Stream { get; }

      Returns the underlying stream for the event.
