.. default-domain:: java
.. highlight:: java

Java Client
===========

This client provides a Java API for interacting with a kRPC server. A jar containing the
``krpc.client`` package can be `downloaded from GitHub <https://github.com/krpc/krpc/releases>`_.
It requires Java 17 or later.

Getting Started
---------------

The kRPC client library depends on the `protobuf
<https://github.com/protocolbuffers/protobuf/tree/main/java>`_ and `javatuples
<https://github.com/arxila/javatuples>`_ libraries. A prebuilt jar for protobuf is available via `Maven
<https://central.sonatype.com/artifact/com.google.protobuf/protobuf-java>`_.

The following example program connects to the server, queries it for its version and prints it out:

.. literalinclude:: /scripts/java/client/Basic.java

To compile this program using javac on the command line, save the source as ``Example.java`` and run
the following:

.. parsed-literal::

   javac -cp krpc-java-|jar-version|.jar:protobuf-java-4.36.2.jar:javatuples-1.2.jar Example.java

Note that you may need to change the paths to the JAR files.

Connecting to the Server
^^^^^^^^^^^^^^^^^^^^^^^^

To connect to a server, call :meth:`Connection.newInstance()` which returns a connection object. All
interaction with the server is done via this object. When constructed without any arguments, it will
connect to the local machine on the default port. You can specify different connection
settings, and also a descriptive name for the connection, as follows:

.. literalinclude:: /scripts/java/client/Connecting.java

.. note::

   A server with "Auto-accept new clients" turned off answers only once the player approves the
   connection, and the client gives up after 5 seconds by default. Pass a ``handshakeTimeout`` of ``Duration.ZERO`` to wait
   for the player.

Calling Remote Procedures
-------------------------

The kRPC server provides *procedures* that a client can run. These procedures are arranged in groups
called *services* to keep things organized. The functionality for the services are defined in the
package ``krpc.client.services``. For example, all of the functionality provided by the
``SpaceCenter`` service is contained in the class ``krpc.client.services.SpaceCenter``.

To interact with a service, you must first instantiate it. You can then call its methods and
properties to invoke remote procedures. The following example demonstrates how to do this. It
instantiates the ``SpaceCenter`` service and calls
:meth:`krpc.client.services.SpaceCenter.SpaceCenter.getActiveVessel()` to get an object representing
the active vessel (of type :type:`krpc.client.services.SpaceCenter.Vessel`). It sets the name of the
vessel and then prints out its altitude:

.. literalinclude:: /scripts/java/client/RemoteProcedures.java

Many procedures return an object identifying something in the game, such as a vessel or
a part. See :doc:`Object Lifetime </user-guide/object-lifetime>` for how such an object
behaves when a game is loaded or what it identifies is destroyed.

Server Side Functions
---------------------

A server side function is code that runs inside the game, on the server. :meth:`Connection.run`
evaluates one once and returns its value. Every value it reads comes from the same physics tick. For
example, the following reads the vessel's altitude and speed together:

.. literalinclude:: /scripts/java/client/RunFunction.java

Use ``run`` for a function with effects, such as setting a property. An event or a function stream
evaluates its function on every update, repeating the effects.

The :rst:ref:`server side functions guide <server-side-functions>` shows how to build
larger functions.

.. _java-client-streams:

Streaming Data
--------------

A common use case for kRPC is to continuously extract data from the game. The naive approach to do
this would be to repeatedly call a remote procedure, such as in the following which repeatedly
prints the position of the active vessel:

.. literalinclude:: /scripts/java/client/Streaming1.java

This approach requires significant communication overhead as request/response messages are
repeatedly sent between the client and server. kRPC provides a more efficient mechanism to achieve
this, called *streams*.

A stream repeatedly executes a procedure on the server (with a fixed set of argument values) and
sends the result to the client. It only requires a single message to be sent to the server to
establish the stream, which will then continuously send data to the client until the stream is
closed.

The following example does the same thing as above using streams:

.. literalinclude:: /scripts/java/client/Streaming2.java

It calls :meth:`Connection.addStream` once at the start of the program to create the stream, and
then repeatedly prints the position returned by the stream. The stream is automatically closed when
the client disconnects.

A stream can be created for any method call by calling :meth:`Connection.addStream` and passing it
information about which method to stream. The example above passes a remote object, the name of the
method to call, followed by the arguments to pass to the method (if
any). :meth:`Connection.addStream` returns a stream object of type :type:`Stream`. The most recent
value of the stream can be obtained by calling :meth:`Stream.get`. A stream can be stopped and
removed from the server by calling :meth:`Stream.remove` on the stream object. All of a clients
streams are automatically stopped when it disconnects.

.. note::

   A stream runs on the server until you remove it or the connection closes, even once the stream
   object is garbage collected. Each call to ``addStream`` adds another stream.

Synchronizing with Stream Updates
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

A common use case for kRPC is to wait until the value returned by a method or attribute changes, and
then take some action. kRPC provides two mechanisms to do this efficiently: *condition variables*
and *callbacks*.

Condition Variables
~~~~~~~~~~~~~~~~~~~

Each stream has a condition variable that is notified whenever the value of the stream changes, and
when the stream ends. You can wait on it to block the current thread until one of these happens.

The following example waits until the abort button is pressed in game, by waiting for the value of
:meth:`krpc.client.services.SpaceCenter.Control.getAbort()` to change to true:

.. literalinclude:: /scripts/java/client/ConditionVariables.java

This code creates a stream, acquires a lock on the streams condition variable (by using a
``synchronized`` block) and then repeatedly checks the value of ``getAbort``. It leaves the loop
when it changes to true.

While a thread holds the lock, the value of the stream changes only when the condition variable is
notified. A check of the value and the wait that follows it therefore see the same updates. A
wait returns for a value newer than the last one the thread read, with or without the lock.

The body of the loop calls ``waitForUpdate`` on the stream, which causes the program to block until
the value changes. This prevents the loop from 'spinning' and so it does not consume processing
resources whilst waiting.

Callbacks
~~~~~~~~~

Streams allow you to register callback functions that are called whenever the value of the stream
changes. Callback functions should take a single argument, which is the new value of the stream, and
should return nothing.

For example the following program registers two callbacks that are invoked when the value of
:meth:`krpc.client.services.SpaceCenter.Control.getAbort()` changes:

.. literalinclude:: /scripts/java/client/Callbacks.java

A callback added to a stream that already has a value is called with that value straight away.

.. note::

   Callbacks run on the client's callback thread, one at a time, and may make remote
   procedure calls. A callback must not wait on a stream, as the same thread wakes the
   stream's waiters. Any changes to shared state must be protected with appropriate
   synchronization.

Stream Endings
^^^^^^^^^^^^^^

The following example streams the temperature of a part until the part is destroyed:

.. literalinclude:: /scripts/java/client/StreamEnd.java

Once a stream ends, getting its value or waiting on it throws the exception that ended it. A stream
ends when its procedure throws an exception, when the server removes it, when you remove it, or when
the connection closes.

A stream that the server removes, such as an event that has finished, ends with
``KRPC.StreamRemovedException``. A stream you remove ends with a ``StreamException``. A closed
connection ends it with a ``StreamException`` whose cause is a ``ConnectionException``.

An exception for an object that is not loaded, or a procedure not available in the game scene,
holds the stream instead. It resumes once the object or scene is available.

.. seealso:: :doc:`/user-guide/object-lifetime`

To handle an ending in a callback, add an end callback with :meth:`Stream.addEndCallback`. It is
called once, with the exception.

Streaming Server Side Functions
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Pass a server side function to :meth:`Connection.addStream` to stream its value. The server
evaluates the function on each stream update, within one physics tick. The value can be any type
the server can send, including collections and objects. For example, the following streams the
vessel's altitude in kilometers:

.. literalinclude:: /scripts/java/client/FunctionStream.java

The type parameter must be able to hold the function's value. Otherwise the assignment of a value
throws a ``ClassCastException``.

A function stream or event ends on any exception its function throws. This includes
``KRPC.ObjectNotLoadedException``, which a game load or an unloaded vessel causes. To keep running
through a load, the function catches that exception with ``Expression.tryCatch``.

.. _java-client-events:

Events
------

Some procedures return event objects of type :type:`Event`. These allow you to wait
until an event occurs, by calling :meth:`Event.waitFor`. Under the hood, these are
implemented using streams and condition variables.

A call to ``waitFor`` returns straight away if the event has fired since the previous call on the
same event object, so no firing is missed between two calls. Several firings between two calls make
``waitFor`` return once.

You can also create a custom event from a server side function. The following builds the function
``MeanAltitude > 1000``, and creates an event that triggers when the function returns true:

.. literalinclude:: /scripts/java/client/CustomEvent.java

Client API Reference
--------------------

.. package:: krpc.client

.. type:: class Connection

   A connection to the kRPC server. All interaction with kRPC is performed via an instance of this
   class.

   .. method:: static Connection newInstance()
   .. method:: static Connection newInstance(String name)
   .. method:: static Connection newInstance(String name, String address)
   .. method:: static Connection newInstance(String name, String address, int port)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address, int port)
   .. method:: static Connection newInstance(String name, String address, int port, java.time.Duration timeout)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address, int port, java.time.Duration timeout)
   .. method:: static Connection newInstance(String name, String address, int port, java.time.Duration timeout, java.time.Duration handshakeTimeout)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address, int port, java.time.Duration timeout, java.time.Duration handshakeTimeout)
   .. method:: static Connection newInstance(String name, String address, int port, java.time.Duration timeout, java.time.Duration handshakeTimeout, java.time.Duration stallTimeout)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address, int port, java.time.Duration timeout, java.time.Duration handshakeTimeout, java.time.Duration stallTimeout)

      Create a connection to the server, using the given connection details.

      :param String name: A descriptive name for the connection. This is passed to the server and
                          appears in the in-game server window.
      :param String address: The address of the server to connect to. Can either be a hostname, an
                             IP address as a string or a :ref:`java.net.InetAddress`
                             object. Defaults to 127.0.0.1.
      :param int port: The port number of the server. Defaults to 50000. This should match the
                       port number of the server you want to connect to.
      :param java.time.Duration timeout: How long to wait for a connection before giving up.
                                         Defaults to zero, which waits indefinitely. A network
                                         that drops a connection attempt rather than refusing it
                                         otherwise leaves the client waiting.
      :param java.time.Duration handshakeTimeout: How long to wait for the server to accept the
                                                  connection before giving up. Defaults to 5
                                                  seconds. Zero waits indefinitely, such as for a
                                                  server that asks the player to approve each
                                                  connection.
      :param java.time.Duration stallTimeout: How long a message from the server can go without a
                                              new byte, once it has started to arrive, before the
                                              connection fails. Defaults to 30 seconds. Zero
                                              waits indefinitely.

      Throws a ``ConnectionException`` if the server refuses the connection. Its ``getStatus``
      method returns the status the server gave, such as ``DENIED``. An exception thrown when the
      server ends the connection returns the reason from its ``getReason`` method, such as
      ``SERVER_STOPPED``.

   .. method:: static Connection newLocalInstance()
   .. method:: static Connection newLocalInstance(String name)
   .. method:: static Connection newLocalInstance(String name, String path)
   .. method:: static Connection newLocalInstance(String name, String path, java.time.Duration handshakeTimeout)
   .. method:: static Connection newLocalInstance(String name, String path, java.time.Duration handshakeTimeout, java.time.Duration stallTimeout)

      Create a connection to a server running on the same machine, over a unix domain socket rather
      than TCP/IP. The connection behaves identically once established.

      Unix domain sockets are available on Linux, macOS, and Windows 10 1803 and Windows
      Server 2019 or later.

      :param String name: A descriptive name for the connection. This is passed to the server and
                          appears in the in-game server window.
      :param String path: The path of the socket the server is listening on. This should match
                          the socket path shown in the in-game server window.
      :param java.time.Duration handshakeTimeout: As for ``newInstance``.
      :param java.time.Duration stallTimeout: As for ``newInstance``.

   .. method:: Stream<T> addStream(Class<?> clazz, String method, Object... args)

      Create a stream for a static method call to the given class.

   .. method:: Stream<T> addStream(RemoteObject instance, String method, Object... args)

      Create a stream for a method call to the given remote object.

   .. method:: Stream<T> addStream(KRPC.Expression function)

      Create a stream from a server side function. Each call creates a new stream. Throws a
      ``StreamException`` if the function has no value, and an ``IllegalArgumentException`` if it
      has parameters.

   .. method:: T run(KRPC.Expression function, Object... args)

      Run a server side function within a single physics tick, and return its value. The value
      is null for a function with no value. The arguments are passed to the function's
      parameters, with a number widened as Java widens it. An ``int`` passes to an unsigned
      parameter bit for bit, and a ``long`` to an unsigned 64-bit parameter, as the client holds
      unsigned values. A ``byte`` or ``short`` that is not negative converts to an unsigned
      parameter, and an ``int`` that is not negative to an unsigned 64-bit parameter. Throws an
      ``IllegalArgumentException`` if the number of arguments differs from the number of
      parameters, or if a parameter cannot take its argument. The server checks a null for a
      parameter that is not nullable, and an object of another class, and these also throw an
      ``IllegalArgumentException``.

   .. method:: Event addEvent(KRPC.Expression function)

      Create an event from a server side function that evaluates to a boolean value. Each call
      creates a new event. Throws an ``IllegalArgumentException`` if the function does not
      evaluate to a boolean value, or has parameters.

   .. method:: krpc.schema.KRPC.ProcedureCall getCall(Class<?> clazz, String method, Object... args)

      Returns a procedure call message for the given static method call. This allows descriptions of
      procedure calls to be passed to the server, for example when constructing custom events. See
      :rst:ref:`java-client-events`.

   .. method:: krpc.schema.KRPC.ProcedureCall getCall(RemoteObject instance, String method, Object... args)

      Returns a procedure call message for the given method call. This allows descriptions of
      procedure calls to be passed to the server, for example when constructing custom events. See
      :rst:ref:`java-client-events`.

   .. method:: void close()

      Tells the server the connection is closing, then closes it.

.. type:: class Stream<T>

   This class represents a stream. See :rst:ref:`java-client-streams`.

   Stream objects implement ``hashCode`` and ``equals`` such that two stream objects are equal if
   they are bound to the same stream on the server. Each call to :meth:`Connection.addStream`
   creates a new stream, so two streams added for the same call are not equal.

   .. method:: float getRate()
   .. method:: void setRate(float rate)

      The update rate of the stream in Hertz. When set to zero, the rate is unlimited.

   .. method:: T get()

      Returns the most recent value for the stream, waiting for the first value if none has
      arrived yet. Throws the exception that ended the stream, if it has ended, such as an
      exception thrown by the remote procedure.

   .. method:: Object getCondition()

      A condition variable that is notified (using ``notifyAll``) whenever the value of the stream
      changes, and when the stream ends.

   .. method:: void waitForUpdate()
   .. method:: boolean waitForUpdateWithTimeout(double timeout)

      These methods block until the value of the stream changes or the operation times out.
      ``waitForUpdateWithTimeout`` returns ``true`` if the value changed, and ``false`` on a
      timeout.

      The streams condition variable must be locked before calling this method.

      If *timeout* is specified it is the timeout in seconds for the operation.

      Throws the exception that ended the stream, if it ends before or during the wait.

   .. method:: int addCallback(java.util.function.Consumer<T> callback)

      Adds a callback function that is invoked whenever the value of the stream changes. The
      callback function should take one argument, which is passed the new value of the
      stream. If the stream already has a value, the callback is invoked with it straight away.
      An error ends the stream and does not invoke the callback. Calling ``get()`` throws it.
      Returns a unique identifier for the callback which can be used to remove it.

      .. note::

         Callbacks run on the client's callback thread, one at a time, and may make remote
         procedure calls. A callback must not wait on a stream, as the same thread wakes the
         stream's waiters. Any changes to shared state must be protected with appropriate
         synchronization.

   .. method:: void removeCallback(int tag)

      Removes a callback function from the stream. The tag is the identifier returned when the
      callback was added.

   .. method:: int addEndCallback(java.util.function.Consumer<Exception> callback)

      Adds a callback function that is invoked once when the stream ends. The callback function
      should take one argument, which is passed the exception that ended the stream. It is invoked
      straight away if the stream has already ended. Returns a unique identifier for the callback
      which can be used to remove it.

   .. method:: void removeEndCallback(int tag)

      Removes an end callback function from the stream. The tag is the identifier returned when
      the callback was added.

   .. method:: void remove()

      Remove the stream from the server. Other streams added for the same call keep running.

.. type:: class Event

   This class represents an event. See :rst:ref:`java-client-events`. It is wrapper around a
   :type:`Stream<Boolean>` that indicates when the event occurs.

   Event objects implement ``hashCode`` and ``equals`` such that two event objects are equal if they
   are bound to the same underlying stream on the server.

   .. method:: Object getCondition()

      The condition variable that is notified (using ``notifyAll``) whenever the event occurs, and
      when the event ends.

   .. method:: void waitFor()
   .. method:: boolean waitForWithTimeout(double timeout)

      These methods block until the event occurs or the operation times out. They return straight
      away if the event has occurred since the previous call on this event object.
      ``waitForWithTimeout`` returns ``true`` if the event occurred, and ``false`` on a timeout.

      The events condition variable must be locked before calling this method.

      If *timeout* is specified it is the timeout in seconds for the operation.

      Throws the exception that ended the event, if it ends before or during the wait.

   .. method:: int addCallback(java.lang.Runnable callback)

      Adds a callback function that is invoked whenever the event occurs. The callback function
      should be a function that takes zero arguments. If the event has occurred since the
      last wait on this event object, and no callback has been invoked for it, the callback is
      invoked straight away. A callback added after the event has ended is not invoked for an
      earlier firing. An error ends the event and does not invoke the callback. Returns an integer
      tag identifying the callback which can be used to remove it later.

   .. method:: void removeCallback(int tag)

      Removes a callback function from the event.

   .. method:: int addEndCallback(java.util.function.Consumer<Exception> callback)

      Adds a callback function that is invoked once when the event ends, with the exception that
      ended it. It is invoked straight away if the event has already ended. Returns an integer tag
      identifying the callback which can be used to remove it later.

   .. method:: void removeEndCallback(int tag)

      Removes an end callback function from the event. The tag is the identifier returned when
      the callback was added.

   .. method:: void remove()

      Removes the event from the server.

   .. method:: Stream<Boolean> getStream()

      Returns the underlying stream for the event.

.. type:: abstract class RemoteObject

   The abstract base class for all remote objects.

   .. method:: static long getObjectId(RemoteObject obj)

      The identifier of a remote object on the server, e.g. to pass to
      :meth:`krpc.client.services.KRPC.Expression.constantObject(Connection, long)`.
      Throws a ``NullPointerException`` if the object is null.
