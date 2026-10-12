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
<https://github.com/google/protobuf/tree/master/java>`_ and `javatuples
<http://www.javatuples.org>`_ libraries. A prebuilt jar for protobuf is available via `Maven
<http://search.maven.org/#search|ga|1|g%3A%22com.google.protobuf%22%20a%3A%22protobuf-java%22>`_.

The following example program connects to the server, queries it for its version and prints it out:

.. literalinclude:: /scripts/client/java/Basic.java

To compile this program using javac on the command line, save the source as ``Example.java`` and run
the following:

.. parsed-literal::

   javac -cp krpc-java-|jar-version|.jar:protobuf-java-4.35.1.jar:javatuples-1.2.jar Example.java

Note that you may need to change the paths to the JAR files.

Connecting to the Server
^^^^^^^^^^^^^^^^^^^^^^^^

To connect to a server, call :meth:`Connection.newInstance()` which returns a connection object. All
interaction with the server is done via this object. When constructed without any arguments, it will
connect to the local machine on the default port numbers. You can specify different connection
settings, and also a descriptive name for the connection, as follows:

.. literalinclude:: /scripts/client/java/Connecting.java

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

.. literalinclude:: /scripts/client/java/RemoteProcedures.java

Many procedures return an object identifying something in the game, such as a vessel or
a part. See :doc:`Object Lifetime </tutorials/object-lifetime>` for how such an object
behaves when a game is loaded or what it identifies is destroyed.

.. _java-client-streams:

Streaming Data
--------------

A common use case for kRPC is to continuously extract data from the game. The naive approach to do
this would be to repeatedly call a remote procedure, such as in the following which repeatedly
prints the position of the active vessel:

.. literalinclude:: /scripts/client/java/Streaming1.java

This approach requires significant communication overhead as request/response messages are
repeatedly sent between the client and server. kRPC provides a more efficient mechanism to achieve
this, called *streams*.

A stream repeatedly executes a procedure on the server (with a fixed set of argument values) and
sends the result to the client. It only requires a single message to be sent to the server to
establish the stream, which will then continuously send data to the client until the stream is
closed.

The following example does the same thing as above using streams:

.. literalinclude:: /scripts/client/java/Streaming2.java

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
---------------------------------

A common use case for kRPC is to wait until the value returned by a method or attribute changes, and
then take some action. kRPC provides two mechanisms to do this efficiently: *condition variables*
and *callbacks*.

Condition Variables
^^^^^^^^^^^^^^^^^^^

Each stream has a condition variable that is notified whenever the value of the stream changes, and
when the stream ends. You can wait on it to block the current thread until one of these happens.

The following example waits until the abort button is pressed in game, by waiting for the value of
:meth:`krpc.client.services.SpaceCenter.Control.getAbort()` to change to true:

.. literalinclude:: /scripts/client/java/ConditionVariables.java

This code creates a stream, acquires a lock on the streams condition variable (by using a
``synchronized`` block) and then repeatedly checks the value of ``getAbort``. It leaves the loop
when it changes to true.

The body of the loop calls ``waitForUpdate`` on the stream, which causes the program to block until
the value changes. This prevents the loop from 'spinning' and so it does not consume processing
resources whilst waiting.

Callbacks
^^^^^^^^^

Streams allow you to register callback functions that are called whenever the value of the stream
changes. Callback functions should take a single argument, which is the new value of the stream, and
should return nothing.

For example the following program registers two callbacks that are invoked when the value of
:meth:`krpc.client.services.SpaceCenter.Control.getAbort()` changes:

.. literalinclude:: /scripts/client/java/Callbacks.java

A callback added to a stream that already has a value is called with that value straight away.

.. note::

   The callback function may be called from a different thread than the one that created the
   stream. Any changes to shared state must therefore be protected with appropriate
   synchronization.

Stream Endings
^^^^^^^^^^^^^^

The following example streams the temperature of a part until the part is destroyed:

.. literalinclude:: /scripts/client/java/StreamEnd.java

Once a stream ends, getting its value or waiting on it throws the exception that ended it. A stream
ends when its procedure throws an exception, when the server removes it, when you remove it, or when
the connection closes.

A stream that the server removes, such as an event that has finished, ends with
``KRPC.StreamRemovedException``. A stream you remove ends with a ``StreamException``. A closed
connection ends it with a ``StreamException`` whose cause is a ``ConnectionException``.

To handle an ending in a callback, add an end callback with :meth:`Stream.addEndCallback`. It is
called once, with the exception.

.. _java-client-events:

Custom Events
-------------

Some procedures return event objects of type :type:`Event`. These allow you to wait
until an event occurs, by calling :meth:`Event.waitFor`. Under the hood, these are
implemented using streams and condition variables.

Custom events can also be created. An expression API allows you to create code that runs on the
server and these can be used to build a custom event. For example, the following creates the
expression ``MeanAltitude > 1000`` and then creates an event that will be triggered when the
expression returns true:

.. literalinclude:: /scripts/client/java/CustomEvent.java

A call to ``waitFor`` returns straight away if the event has fired since the previous call on the
same event object, so no firing is missed between two calls. Several firings between two calls make
``waitFor`` return once.

Client API Reference
--------------------

.. package:: krpc.client

.. type:: class Connection

   A connection to the kRPC server. All interaction with kRPC is performed via an instance of this
   class.

   .. method:: static Connection newInstance()
   .. method:: static Connection newInstance(String name)
   .. method:: static Connection newInstance(String name, String address)
   .. method:: static Connection newInstance(String name, String address, int rpcPort, int streamPort)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address, int rpcPort, int streamPort)
   .. method:: static Connection newInstance(String name, String address, int rpcPort, int streamPort, java.time.Duration timeout)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address, int rpcPort, int streamPort, java.time.Duration timeout)

      Create a connection to the server, using the given connection details.

      :param String name: A descriptive name for the connection. This is passed to the server and
                          appears in the in-game server window.
      :param String address: The address of the server to connect to. Can either be a hostname, an
                             IP address as a string or a :ref:`java.net.InetAddress`
                             object. Defaults to 127.0.0.1.
      :param int rpc_port: The port number of the RPC Server. Defaults to 50000. This should match
                           the RPC port number of the server you want to connect to.
      :param int stream_port: The port number of the Stream Server. Defaults to 50001. This should
                              match the stream port number of the server you want to connect to.
      :param java.time.Duration timeout: How long to wait for a connection before giving up.
                                         Defaults to zero, which waits indefinitely. A network
                                         that drops a connection attempt rather than refusing it
                                         otherwise leaves the client waiting.

   .. method:: static Connection newLocalInstance()
   .. method:: static Connection newLocalInstance(String name)
   .. method:: static Connection newLocalInstance(String name, String rpcPath, String streamPath)

      Create a connection to a server running on the same machine, over unix domain sockets rather
      than TCP/IP. The connection behaves identically once established.

      Unix domain sockets are available on Linux, macOS, and Windows 10 1803 and Windows
      Server 2019 or later.

      :param String name: A descriptive name for the connection. This is passed to the server and
                          appears in the in-game server window.
      :param String rpcPath: The path of the socket the RPC Server is listening on. This should
                             match the RPC socket path shown in the in-game server window.
      :param String streamPath: The path of the socket the Stream Server is listening on. This
                                should match the stream socket path shown in the in-game server
                                window.

   .. method:: Stream<T> addStream(Class<?> clazz, String method, Object... args)

      Create a stream for a static method call to the given class.

   .. method:: Stream<T> addStream(RemoteObject instance, String method, Object... args)

      Create a stream for a method call to the given remote object.

   .. method:: krpc.schema.KRPC.ProcedureCall getCall(Class<?> clazz, String method, Object... args)

      Returns a procedure call message for the given static method call. This allows descriptions of
      procedure calls to be passed to the server, for example when constructing custom events. See
      :rst:ref:`java-client-events`.

   .. method:: krpc.schema.KRPC.ProcedureCall getCall(RemoteObject instance, String method, Object... args)

      Returns a procedure call message for the given method call. This allows descriptions of
      procedure calls to be passed to the server, for example when constructing custom events. See
      :rst:ref:`java-client-events`.

   .. method:: void close()

      Close the connection.

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

         The callback function may be called from a different thread than the one that created the
         stream. Any changes to shared state must therefore be protected with appropriate
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
