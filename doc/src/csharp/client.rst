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

Many procedures return an object identifying something in the game, such as a vessel or
a part. See :doc:`Object Lifetime </tutorials/object-lifetime>` for how such an object
behaves when a game is loaded or what it identifies is destroyed.

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
a lambda expression that invokes the desired method. This lambda expression must take zero arguments
and be a method call or property access, which may be multiplied by a constant. It returns a stream
object of type :type:`Stream`. The most recent value of the stream can be obtained by calling
:meth:`Stream.Get`. A stream can be stopped and removed from the server by calling
:meth:`Stream.Remove` on the stream object. All of a clients streams are automatically stopped when
it disconnects.

.. note::

   A stream runs on the server until you remove it or the connection closes, even once the stream
   object is garbage collected. Each call to ``AddStream`` adds another stream.

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

Callbacks
^^^^^^^^^

Streams allow you to register callback functions that are called whenever the value of the stream
changes. Callback functions should take a single argument, which is the new value of the stream, and
should return nothing.

For example the following program registers two callbacks that are invoked when the value of
:prop:`KRPC.Client.Services.SpaceCenter.Control.Abort` changes:

.. literalinclude:: /scripts/client/csharp/Callbacks.cs

A callback added to a stream that already has a value is called with that value straight away.

.. note::

   The callback function may be called from a different thread than the one that created the
   stream. Any changes to shared state must therefore be protected with appropriate
   synchronization.

Stream Endings
^^^^^^^^^^^^^^

The following example streams the temperature of a part until the part is destroyed:

.. literalinclude:: /scripts/client/csharp/StreamEnd.cs

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

.. _csharp-client-events:

Events
------

Some procedures return event objects of type :type:`Event`. These allow you to wait until an event
occurs, by calling :meth:`Event.Wait`. Under the hood, these are implemented using streams and
condition variables.

A call to ``Wait`` returns straight away if the event has fired since the previous call on the same
event object, so no firing is missed between two calls. Several firings between two calls make
``Wait`` return once.

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

      Create a new stream from the given lambda expression.

   .. method:: KRPC.Schema.KRPC.ProcedureCall GetCall(LambdaExpression expression)

      Returns a procedure call message for the given lambda expression. This allows descriptions of
      procedure calls to be passed to the server.

   .. method:: void Dispose()

      Closes the connection and frees the resources associated with it.

.. class:: Stream<ReturnType>

   This class represents a stream. See :ref:`csharp-client-streams`.

   Stream objects implement ``GetHashCode``, ``Equals``, ``operator ==`` and ``operator !=`` such
   that two stream objects are equal if they are bound to the same stream on the server. Each call
   to :meth:`Connection.AddStream` creates a new stream, so two streams of the same call are not
   equal.

   .. property:: float Rate { get; set; }

      The update rate of the stream in Hertz. When set to zero, the rate is unlimited.

   .. method:: ReturnType Get()

      Returns the most recent value for the stream, waiting for the first value if none has arrived
      yet. Throws the exception that ended the stream, if it has ended, such as an exception thrown
      by the remote procedure.

   .. property:: object Condition { get; }

      A condition variable that is notified (using ``Monitor.PulseAll``) whenever the value of the
      stream changes.

   .. method:: bool Wait(double timeout = -1)

      This method blocks until the value of the stream changes or the operation times out. Returns
      ``true`` if the value changed, and ``false`` on a timeout.

      The streams condition variable must be locked before calling this method.

      If *timeout* is specified and is greater than or equal to 0, it is the timeout in seconds for
      the operation. Throws the exception that ended the stream, if it ends before or during the
      wait.

   .. method:: int AddCallback(Action<ReturnType> callback)

      Adds a callback function that is invoked whenever the value of the stream changes. The
      callback function should take one argument, which is passed the new value of the
      stream. If the stream already has a value, the callback is invoked with it straight away.
      An error ends the stream and does not invoke the callback. Calling ``Get()`` throws it.
      Returns a unique identifier for the callback which can be used to remove it.

      .. note::

         The callback function may be called from a different thread than the one that created the
         stream. Any changes to shared state must therefore be protected with appropriate
         synchronization.

   .. method:: void RemoveCallback(int tag)

      Removes a callback from the stream. The tag is the identifier returned when the callback was
      added.

   .. method:: int AddEndCallback(Action<System.Exception> callback)

      Adds a callback function that is invoked once when the stream ends. The callback function
      should take one argument, which is passed the exception that ended the stream. It is invoked
      straight away if the stream has already ended. Returns a unique identifier for the callback
      which can be used to remove it.

   .. method:: void RemoveEndCallback(int tag)

      Removes an end callback from the stream. The tag is the identifier returned when the callback
      was added.

   .. method:: void Remove()

      Removes the stream from the server. Other streams of the same call are not affected.

.. class:: Event

   This class represents an event. See :ref:`csharp-client-events`. It is wrapper around a
   :type:`Stream<bool>` that indicates when the event occurs.

   Event objects implement ``GetHashCode``, ``Equals``, ``operator ==`` and ``operator !=`` such
   that two event objects are equal if they are bound to the same underlying stream on the server.

   .. property:: object Condition { get; }

      The condition variable that is notified (using ``Monitor.PulseAll``) whenever the event
      occurs.

   .. method:: bool Wait(double timeout = -1)

      This method blocks until the event occurs or the operation times out. It returns straight
      away if the event has occurred since the previous call on this event object. Returns ``true``
      if the event occurred, and ``false`` on a timeout.

      The events condition variable must be locked before calling this method.

      If *timeout* is specified and is greater than or equal to 0, it is the timeout in seconds for
      the operation. Throws the exception that ended the event, if it ends before or during the
      wait.

   .. method:: int AddCallback(Action callback)

      Adds a callback function that is invoked whenever the event occurs. The callback function
      should be a function that takes zero arguments. If the event has not ended and has fired
      since the last ``Wait`` on this event object returned and since its callbacks were last
      invoked, the callback is invoked once straight away. An error ends the event and does not
      invoke the callback. Returns a unique identifier for the callback which can be used to
      remove it.

   .. method:: void RemoveCallback(int tag)

      Removes a callback from the event. The tag is the identifier returned when the callback was
      added.

   .. method:: int AddEndCallback(Action<System.Exception> callback)

      Adds a callback function that is invoked once when the event ends, with the exception that
      ended it. It is invoked straight away if the event has already ended. Returns a unique
      identifier for the callback which can be used to remove it.

   .. method:: void RemoveEndCallback(int tag)

      Removes an end callback from the event. The tag is the identifier returned when the callback
      was added.

   .. method:: void Remove()

      Removes the event from the server.

   .. property:: Stream<bool> Stream { get; }

      Returns the underlying stream for the event.
