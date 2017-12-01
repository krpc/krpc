.. default-domain:: java
.. highlight:: java

Java Client
===========

This client provides a Java API for interacting with a kRPC server. A jar containing the
``krpc.client`` package can be :github-download-jar:`downloaded from GitHub <krpc-java>`. It
requires Java version 1.8.

Using the Library
-----------------

The kRPC client library depends on the `protobuf
<https://github.com/google/protobuf/tree/master/java>`_ and `javatuples
<http://www.javatuples.org>`_ libraries. A prebuilt jar for protobuf is available via `Maven
<http://search.maven.org/#search|ga|1|g%3A%22com.google.protobuf%22%20a%3A%22protobuf-java%22>`_. Note
that you need protobuf version 3. Version 2 is not compatible with kRPC.

The following example program connects to the server, queries it for its version and prints it out:

.. literalinclude:: /scripts/client/java/Basic.java

To compile this program using javac on the command line, save the source as ``Example.java`` and run
the following:

.. code-block:: bash

   javac -cp krpc-java-0.4.0.jar:protobuf-java-3.4.0.jar:javatuples-1.2.jar Example.java

You may need to change the paths to the JAR files.

Connecting to the Server
------------------------

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

.. _java-client-streams:

Streaming Data from the Server
------------------------------

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
:meth:`krpc.client.services.SpaceCenter.Control.getAbort()` to change to true:

.. literalinclude:: /scripts/client/java/ConditionVariables.java

This code creates a stream, acquires a lock on the streams condition variable (by using a
``synchronized`` block) and then repeatedly checks the value of ``getAbort``. It leaves the loop
when it changes to true.

The body of the loop calls ``waitForUpdate`` on the stream, which causes the program to block until
the value changes. This prevents the loop from 'spinning' and so it does not consume processing
resources whilst waiting.

.. note::

   The stream does not start receiving updates until the first call to ``waitForUpdate``. This means
   that the example code will not miss any updates to the streams value, as it will have already
   locked the condition variable before the first stream update is received.

Callbacks
^^^^^^^^^

Streams allow you to register callback functions that are called whenever the value of the stream
changes. Callback functions should take a single argument, which is the new value of the stream, and
should return nothing.

For example the following program registers two callbacks that are invoked when the value of
:meth:`krpc.client.services.SpaceCenter.Control.getAbort()` changes:

.. literalinclude:: /scripts/client/java/Callbacks.java

.. note::

   When a stream is created it does not start receiving updates until ``start`` is called. This is
   implicitly called when accessing the value of a stream, but as this example does not do this an
   explicit call to ``start`` is required.

.. note::

   The callbacks are registered before the call to ``start`` so that stream updates are not missed.

.. note::

   The callback function may be called from a different thread to that which created the stream. Any
   changes to shared state must therefore be protected with appropriate synchronization.

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
   they are bound to the same stream on the server.

   .. method:: void start()
   .. method:: void startAndWait()

      Starts the stream. When a stream is created it does not start sending updates to the client
      until this method is called.

      The ``startAndWait`` method will block until at least one update has been received from the
      server.

      The ``start`` method starts the stream and returns immediately. Subsequent calls to ``get()``
      may throw a ``StreamException``.

   .. method:: float getRate()
   .. method:: void setRate(float rate)

      The update rate of the stream in Hertz. When set to zero, the rate is unlimited.

   .. method:: T get()

      Returns the most recent value for the stream. If executing the remote procedure for the stream
      throws an exception, calling this method will rethrow the exception. Raises a
      ``StreamException`` if no update has been received from the server.

      If the stream has not been started this method calls ``startAndWait()`` to start the stream
      and wait until at least one update has been received.

   .. method:: Object getCondition()

      A condition variable that is notified (using ``notifyAll``) whenever the value of the stream
      changes.

   .. method:: void waitForUpdate()
   .. method:: void waitForUpdateWithTimeout(double timeout)

      These methods block until the value of the stream changes or the operation times out.

      The streams condition variable must be locked before calling this method.

      If *timeout* is specified it is the timeout in seconds for the operation.

      If the stream has not been started this method calls ``start`` to start the stream (without
      waiting for at least one update to be received).

   .. method:: void addCallback(java.util.function.Consumer<T> callback)

      Adds a callback function that is invoked whenever the value of the stream changes. The
      callback function should take one argument, which is passed the new value of the stream.

      .. note::

         The callback function may be called from a different thread to that which created the
         stream. Any changes to shared state must therefore be protected with appropriate
         synchronization.

   .. method:: void remove()

      Remove the stream from the server.

.. type:: class Event

   This class represents an event. See :rst:ref:`java-client-events`. It is wrapper around a
   :type:`Stream<Boolean>` that indicates when the event occurs.

   Event objects implement ``hashCode`` and ``equals`` such that two event objects are equal if they
   are bound to the same underlying stream on the server.

   .. method:: void start()

      Starts the event. When an event is created, it will not receive updates from the server until
      this method is called.

   .. method:: Object getCondition()

      The condition variable that is notified (using ``notifyAll``) whenever the event occurs.

   .. method:: void waitFor()
   .. method:: void waitForWithTimeout(double timeout)

      These methods block until the event occurs or the operation times out.

      The events condition variable must be locked before calling this method.

      If *timeout* is specified it is the timeout in seconds for the operation.

      If the event has not been started this method calls ``start()`` to start the underlying
      stream.

   .. method:: void addCallback(java.lang.Callable callback)

      Adds a callback function that is invoked whenever the event occurs. The callback function
      should be a function that takes zero arguments.

   .. method:: void remove()

      Removes the event from the server.

   .. method:: Stream<Boolean> getStream()

      Returns the underlying stream for the event.

.. type:: abstract class RemoteObject

   The abstract base class for all remote objects.
