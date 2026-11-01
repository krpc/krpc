.. default-domain:: java
.. highlight:: java

Java
====

This guide describes how to install the Java client, and the features it provides. The
:std:ref:`java-client-reference` describes each class and method.

Installing the Library
----------------------

The library is a jar containing the ``krpc.client`` package, which can be `downloaded from GitHub
<https://github.com/krpc/krpc/releases>`_. It requires Java 17 or later.

The library depends on the `protobuf <https://github.com/protocolbuffers/protobuf/tree/main/java>`_
and `javatuples <https://github.com/arxila/javatuples>`_ libraries. A prebuilt jar for protobuf is
available from `Maven <https://central.sonatype.com/artifact/com.google.protobuf/protobuf-java>`_.

To compile a program with javac, add the three jars to the class path:

.. code-block:: bash

   javac -cp krpc-java.jar:protobuf-java.jar:javatuples.jar Example.java

The paths are those of the jars on your machine.

Connecting to the Server
------------------------

The game must be running with the server started. :std:ref:`getting-started-server` in the getting
started guide shows how to install and start it.

The client connects over TCP/IP or over a local socket. Each server in the server window uses one
protocol, and accepts connections over that protocol only. Pick the protocol when you add the
server, and connect with the matching method.

.. note::

   A server with "Auto-accept new clients" turned off answers only once the player approves the
   connection in game. The connection fails after 5 seconds by default. Pass a longer
   ``handshakeTimeout``, such as ``Duration.ofMinutes(5)``, to give the player time to approve it.

Over TCP/IP
^^^^^^^^^^^

Select "Protobuf over TCP/IP" as the server's protocol. :meth:`~Connection.newInstance` connects to
it, and returns a :type:`~krpc.client.Connection`. You make every call through this object. The
following connects to a server with the default settings, address ``127.0.0.1`` and port 50000, and
prints its version:

.. literalinclude:: /scripts/java/Connecting1.java

:meth:`~Connection.newInstance` also takes a name for the connection, then the address and port of
the server. These match the address and port shown in the server window. The name appears in the
server window in the game:

.. literalinclude:: /scripts/java/Connecting2.java

TCP/IP works over a network, so your program can run on a different machine from the game.

Over a Local Socket
^^^^^^^^^^^^^^^^^^^

Select "Protobuf over local socket" as the server's protocol. :meth:`~Connection.newLocalInstance`
connects to it, and returns a :type:`~krpc.client.Connection` as :meth:`~Connection.newInstance`
does. The following connects to a server with the default settings, at the default socket path for
your platform, and prints its version:

.. literalinclude:: /scripts/java/ConnectingLocal.java

:meth:`~Connection.newLocalInstance` also takes a ``path`` after the name, for a server with another
socket path. The path matches the one shown in the server window.

A local socket works only on the machine running the game. It has lower overhead than TCP/IP, so it
is faster.

Closing the Connection
^^^^^^^^^^^^^^^^^^^^^^

A :type:`~krpc.client.Connection` is :type:`~java.lang.AutoCloseable`. Open it in a
try-with-resources statement, as the examples in this guide do, to close it at the end of the block.
:meth:`~Connection.close` closes a connection explicitly:

.. literalinclude:: /scripts/java/Connecting3.java

Calling Remote Procedures
-------------------------

The server provides *procedures* for your program to call, grouped into *services*.

The following gets the active vessel from
:meth:`~krpc.client.services.SpaceCenter.SpaceCenter.getActiveVessel()`, an object of type
:type:`~krpc.client.services.SpaceCenter.Vessel`. It sets the vessel's name, then prints its
altitude:

.. literalinclude:: /scripts/java/RemoteProcedures.java

Each service is a class in the ``krpc.client.services`` package, such as
:type:`~krpc.client.services.SpaceCenter.SpaceCenter`. Its static ``newInstance`` method takes the
connection, and returns an object for the service. A property is a pair of methods, such as
``getName`` and ``setName``.

Handling Errors
---------------

A procedure that fails on the server throws an exception in your program. The following catches the
error from loading a save that does not exist:

.. literalinclude:: /scripts/java/Errors.java

The exception you catch depends on the error:

* A standard error, such as an invalid argument, throws the matching Java exception. The example
  catches :type:`~java.lang.IllegalArgumentException`, as a missing save is a
  :type:`~krpc.client.services.KRPC.ArgumentException` on the server.
* An error particular to a service throws a class of that service, such as
  :type:`~krpc.client.services.KRPC.ObjectDestroyedException`.
* An error the client has no class for throws :type:`~krpc.client.RPCException`.

The standard Java exceptions are unchecked. The exception classes of the services derive from
:type:`~krpc.client.RPCException`, which is checked. Every remote procedure declares
``throws RPCException``, and connecting declares ``throws IOException``.

Remote Objects
--------------

Many procedures return an object identifying something in the game, such as a vessel or a part. The
object holds no copy of the game's state. It is just a reference to it. Each call through it reads
the game as it is now:

.. literalinclude:: /scripts/java/RemoteObjects.java

Two objects for the same thing in the game are equal, and have the same ``hashCode``. They can be
used as the keys of a :type:`~java.util.HashMap`. Remote objects are also
:type:`~java.lang.Comparable`, so they can be sorted and used as the keys of a
:type:`~java.util.TreeMap`.

Once the thing an object identifies is destroyed, such as a part that explodes, each call through it
throws :type:`~krpc.client.services.KRPC.ObjectDestroyedException`.

An object for a vessel or a part keeps working when you load a save, quickload or revert, and then
reads the loaded game. Other objects depend on what they identify. A maneuver node, for example, is
destroyed by a load.

A vessel far from the active vessel is *unloaded*, and the game keeps no parts for it. The vessel
itself reads normally, but a call on one of its parts throws
:type:`~krpc.client.services.KRPC.ObjectNotLoadedException`. The part works again once the vessel
comes back into range.

.. seealso:: :doc:`/user-guide/object-lifetime` describes how each kind of object behaves in more
             detail.

.. _java-client-streams:

Streaming Data
--------------

A common use of kRPC is to read data from the game continuously. The following does so by calling a
remote procedure in a loop, printing the position of the active vessel:

.. literalinclude:: /scripts/java/Streaming1.java

Each call sends a request to the server and waits for its response. A *stream* is more efficient.
The server runs the call repeatedly, and sends your program the result each time it changes.

The following does the same as above using a stream:

.. literalinclude:: /scripts/java/Streaming2.java

:meth:`~Connection.addStream` creates the stream from a remote object, the name of a method, and its
arguments. It returns a :type:`~krpc.client.Stream`, whose type parameter is the type of the
method's value. :meth:`~Stream.get` returns the stream's latest value.

A property is streamed with the name of its getter:

.. literalinclude:: /scripts/java/Streaming3.java

A procedure of a service is streamed with the service's class in place of an object, such as
``connection.addStream(SpaceCenter.class, "getUT")``.

A stream can be made of any procedure or property that returns a value. Each call to
:meth:`~Connection.addStream` creates another stream.

A stream runs until you remove it with :meth:`~Stream.remove`, your program drops its last
reference to it, or the connection closes. Keep a reference to a stream for as long as you need its
values or its callbacks.

By default, the server sends a stream's value each time it changes. Call :meth:`~Stream.setRate` to
limit how many times a second the stream updates:

.. literalinclude:: /scripts/java/StreamRate.java

Waiting for Changes
^^^^^^^^^^^^^^^^^^^

Each stream has a condition variable, the object :meth:`~Stream.getCondition` returns.
:meth:`~Stream.waitForUpdate` blocks until the stream's value changes. The following waits for the
abort button to be pressed:

.. literalinclude:: /scripts/java/ConditionVariables.java

Hold the condition when you call :meth:`~Stream.waitForUpdate`, as the ``synchronized`` block does
here. While you hold it, the value changes only when you wait, so the loop misses no updates. An
ending reaches you the same way: the wait returns, and your next read or wait throws the exception
that ended the stream.

:meth:`~Stream.waitForUpdateWithTimeout` takes a timeout in seconds, and returns ``false`` when it
elapses before an update.

Callbacks
^^^^^^^^^

A callback is a function the client calls with each new value of a stream. The following adds two
callbacks to a stream of the abort button:

.. literalinclude:: /scripts/java/Callbacks.java

:meth:`~Stream.addCallback` returns a tag, which :meth:`~Stream.removeCallback` takes to remove the
callback.

A callback added to a stream that already has a value is called with that value straight away. It is
then called with each new value, in order.

.. note::

   Each connection has a callback thread, which runs the callbacks of its streams one at a time. A
   callback can make remote procedure calls. Other callbacks and waits are held up while it runs, so
   return promptly, and do not wait on a stream in a callback. For a long running task, start a
   worker thread from the callback.

.. _java-client-stream-endings:

Stream Endings
^^^^^^^^^^^^^^

A stream ends when its procedure throws an exception, when the server removes it, when you remove
it, or when the connection closes. Getting the stream's value or waiting on it then throws the
exception that ended it. The following streams the temperature of a part until the part is
destroyed:

.. literalinclude:: /scripts/java/StreamEnd.java

The exception is the one the procedure threw, or one of the following:

* :type:`~krpc.client.services.KRPC.StreamRemovedException` when the server removes the stream,
  such as for an :std:ref:`event <java-client-events>` that has finished.
* :type:`~krpc.client.StreamException` when you remove the stream from the client.

An exception for an object that is not loaded, or for a procedure not available in the current game
scene, does not end the stream. It pauses the stream's updates, and the stream keeps its last value.
The updates resume once the object or the scene is available.

.. seealso:: :doc:`/user-guide/object-lifetime` describes when each kind of object is not loaded,
             and how streams behave around a load.

:meth:`~Stream.addEndCallback` adds a callback that is called once when the stream ends, with the
exception. The following prints the temperature of a part with a callback, and prints the exception
when the stream ends:

.. literalinclude:: /scripts/java/EndCallback.java

Server Side Functions
---------------------

A server side function runs inside the game. You build it in your program with the expression API,
and the client sends it to the server. The following reads the vessel's altitude and speed together:

.. literalinclude:: /scripts/java/RunFunction.java

:meth:`~Connection.getCall` turns a call into a message, and the static methods of
:type:`~krpc.client.services.KRPC.Expression` build the function from it. :std:ref:`The Expression
API <server-side-functions-api>` in the server side functions guide describes these methods, and
shows how to build larger functions.

Running a Function
^^^^^^^^^^^^^^^^^^

:meth:`~Connection.run` runs a function once, and returns its value. The server runs the whole
function within one physics tick, so every value it reads comes from the same moment in the game. In
the example, the altitude and the speed are consistent with each other, which two separate calls
cannot guarantee.

Streaming a Function
^^^^^^^^^^^^^^^^^^^^

Pass a function to :meth:`~Connection.addStream` to stream its value. The server evaluates the
function on each update, within one physics tick. The following streams the vessel's altitude in
kilometers:

.. literalinclude:: /scripts/java/FunctionStream.java

The stream's type parameter must hold the function's value. Otherwise assigning a value throws a
:type:`~java.lang.ClassCastException`.

A function stream ends on an exception its function throws, just like an :std:ref:`ordinary stream
<java-client-stream-endings>`. Unlike an ordinary stream, it also ends on
:type:`~krpc.client.services.KRPC.ObjectNotLoadedException`, thrown while the game loads or when a
vessel unloads. The server cannot pause a function partway through, and running it again would
repeat its effects. To keep a function running through a load, catch the exception in the function:

.. literalinclude:: /scripts/java/NotLoadedStream.java
   :language: java
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

.. _java-client-events:

Events
------

A common use of kRPC is to wait for something to happen in the game, such as the vessel reaching an
altitude. An *event* does this efficiently. The server checks a condition, written as a server side
function, and sends your program an update when it becomes true. The following waits until the
vessel's altitude exceeds 1000 meters:

.. literalinclude:: /scripts/java/CustomEvent.java

:meth:`~Connection.addEvent` creates the event, and :meth:`~Event.waitFor` waits for it to fire. The
server checks the condition every physics tick, so the event catches a condition that holds for only
a moment, which a loop of calls can miss.

The event fires again each time the condition becomes true after being false. Call
:meth:`~Event.remove` to stop it.

As with a stream, hold the event's condition when you call :meth:`~Event.waitFor`. A wait returns
straight away if the event has fired since your last wait, so a firing between two waits is not
missed.

Event Callbacks
^^^^^^^^^^^^^^^

An event can also call a callback each time it fires. The callback is a :type:`~java.lang.Runnable`:

.. literalinclude:: /scripts/java/EventCallback.java

Callbacks for events run on the connection's callback thread, as the callbacks of a stream do.

Event Endings
^^^^^^^^^^^^^

An event ends in the same ways as a :std:ref:`stream <java-client-stream-endings>`. Waiting on an
event that has ended throws the exception that ended it.

When the event fires just before the server ends it, your next wait returns for that firing first.
Removing the event, dropping your program's last reference to it, or closing the connection, ends
it at once.

:meth:`~Event.addEndCallback` adds a callback that is called once when the event ends, with the
exception, just like an ordinary stream.

Threading
---------

A connection can be shared between threads. Each thread makes calls over it as it would alone, and
each call returns its own result. The following prints the altitude and speed of the vessel from two
threads:

.. literalinclude:: /scripts/java/Threads.java

Streams and events can also be used from any thread:

* Several threads can read a stream, or wait on a stream or an event. A change wakes every thread
  waiting for it.
* A thread holding a stream's condition does not stop other threads from making calls.
* Callbacks run on the connection's callback thread, one at a time, and never on your program's
  threads. A callback added to a stream that already has a value is different. Its first call, with
  that value, might run on your thread, before :meth:`~Stream.addCallback` returns.
  Protect state that a callback shares with your program's threads, such as with a
  ``synchronized`` block.

A wait on a stream or an event that another thread removes throws a
:type:`~krpc.client.StreamException`. Interrupting a waiting thread also ends its wait with a
:type:`~krpc.client.StreamException`, and the stream or event runs on.

Deprecated Members
------------------

A member of a service can be deprecated, when a newer member replaces it. A deprecated member still
works, but may be removed in a future release, so move to its replacement.

The API reference and the member's documentation comment mark it as deprecated, and name the
replacement. The member carries the ``@Deprecated`` annotation, so ``javac`` prints a note when a
program uses it. Compile with ``-Xlint:deprecation`` to list each use.

Service Stubs
-------------

The client includes code for each service that ships with kRPC, generated from the services of
one kRPC release and compiled into the client's jar. These *stubs* are the classes in the
``krpc.client.services`` package, such as :type:`~krpc.client.services.SpaceCenter.SpaceCenter`.

The stubs match the server from the same release, so install the client version that matches your
server. With another version, a procedure that was removed or renamed throws an exception when
you call it. A parameter or return type that changed is not checked, so a call can send or return
a wrong value with no error.

A service with no stub, such as one added by another mod, needs stubs of its own.
:std:ref:`service-api-clientgen` shows how to generate them.

.. _java-client-reference:

Client API Reference
--------------------

.. package:: krpc.client

.. type:: class Connection

   A connection to the server. Obtain one by calling :meth:`newInstance` or
   :meth:`newLocalInstance`. It is :type:`~java.lang.AutoCloseable`, so a try-with-resources
   statement closes it at the end of the block.

   .. method:: static Connection newInstance()
   .. method:: static Connection newInstance(String name)
   .. method:: static Connection newInstance(String name, String address)
   .. method:: static Connection newInstance(String name, String address, int port)
   .. method:: static Connection newInstance(String name, String address, int port, java.time.Duration timeout)
   .. method:: static Connection newInstance(String name, String address, int port, java.time.Duration timeout, java.time.Duration handshakeTimeout)
   .. method:: static Connection newInstance(String name, String address, int port, java.time.Duration timeout, java.time.Duration handshakeTimeout, java.time.Duration stallTimeout)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address, int port)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address, int port, java.time.Duration timeout)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address, int port, java.time.Duration timeout, java.time.Duration handshakeTimeout)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address, int port, java.time.Duration timeout, java.time.Duration handshakeTimeout, java.time.Duration stallTimeout)

      Connects to a kRPC server over TCP/IP.

      :param String name: A name for the connection, shown in the server window in the game.
      :param String address: The address of the server, as a host name, an IP address, or a
                             :type:`~java.net.InetAddress`. Defaults to ``127.0.0.1``.
      :param int port: The port of the server. Defaults to 50000.
      :param java.time.Duration timeout: How long to wait for the connection to open. Defaults to
                                         zero, which waits indefinitely. Without a timeout, a
                                         network that drops the connection attempt leaves the
                                         client waiting.
      :param java.time.Duration handshakeTimeout: How long to wait for the server to accept the
                                                  connection. Defaults to 5 seconds.
      :param java.time.Duration stallTimeout: How long a message from the server can go without a
                                              new byte, once it has started to arrive, before the
                                              connection fails. Defaults to 30 seconds.

      Throws a :type:`ConnectionException` if the connection fails or the server refuses it, and
      an :type:`~java.lang.IllegalArgumentException` for a negative timeout, or a handshake or
      stall timeout of zero.

   .. method:: static Connection newLocalInstance()
   .. method:: static Connection newLocalInstance(String name)
   .. method:: static Connection newLocalInstance(String name, String path)
   .. method:: static Connection newLocalInstance(String name, String path, java.time.Duration handshakeTimeout)
   .. method:: static Connection newLocalInstance(String name, String path, java.time.Duration handshakeTimeout, java.time.Duration stallTimeout)

      Connects to a kRPC server on the same machine over a local socket. The server must use the
      local socket protocol.

      Local sockets are available on Linux, macOS, and Windows 10 1803 and Windows Server 2019 or
      later.

      :param String name: A name for the connection, shown in the server window in the game.
      :param String path: The path of the server's socket, shown in the server window in the game.
                          Defaults to the path the server uses unless it was configured with
                          another.
      :param java.time.Duration handshakeTimeout: As for :meth:`newInstance`.
      :param java.time.Duration stallTimeout: As for :meth:`newInstance`.

      Throws a :type:`ConnectionException` and an :type:`~java.lang.IllegalArgumentException` as
      :meth:`newInstance` does.

   .. method:: void close()

      Closes the connection. Closing a connection that is already closed does nothing.

   .. method:: <T> Stream<T> addStream(RemoteObject instance, String method, Object... args)

      Creates a stream for a call to the method named *method* of the remote object *instance*,
      with arguments *args*, and returns a :type:`Stream`. Each call creates a new stream. Stream
      a property by passing the name of its getter.

      Throws an :type:`RPCException` if the object has no method of that name taking the
      arguments.

   .. method:: <T> Stream<T> addStream(Class<?> clazz, String method, Object... args)

      Creates a stream for a call to the method named *method* of the class *clazz*, with
      arguments *args*, and returns a :type:`Stream`. The class is a service, such as
      ``SpaceCenter.class``, or a class with a static method. For a static method, pass the
      connection as the first argument. Each call creates a new stream.

      Throws an :type:`RPCException` if the class has no method of that name taking the
      arguments.

   .. method:: <T> Stream<T> addStream(KRPC.Expression function)

      Creates a stream from a server side function, evaluated on each update, and returns a
      :type:`Stream`. Each call creates a new stream. The type parameter must hold the function's
      value, or assigning a value throws a :type:`~java.lang.ClassCastException`.

      Throws a :type:`StreamException` if the function has no value, and an
      :type:`~java.lang.IllegalArgumentException` if it has parameters.

   .. method:: <T> T run(KRPC.Expression function, Object... args)

      Runs a server side function within a single physics tick, and returns its value. The value is
      ``null`` for a function with no value, and for a null value. The arguments are passed to the
      function's parameters, with a number widened as Java widens it. An ``int`` or a ``long``
      passes to an unsigned parameter of its width bit for bit, as the client holds unsigned values
      in signed types.

      Throws an :type:`~java.lang.IllegalArgumentException` if the number of arguments differs from
      the number of parameters, or if a parameter cannot take its argument.

   .. method:: Event addEvent(KRPC.Expression function)

      Creates an event from a server side function that evaluates to a boolean value, and returns
      an :type:`Event`. Each call creates a new event.

      Throws an :type:`~java.lang.IllegalArgumentException` if the function does not evaluate to a
      boolean value, or has parameters.

   .. method:: krpc.schema.KRPC.ProcedureCall getCall(RemoteObject instance, String method, Object... args)
   .. method:: krpc.schema.KRPC.ProcedureCall getCall(Class<?> clazz, String method, Object... args)

      Converts a call to a method of a remote object, or of a class as for :meth:`addStream`, into
      a procedure call message. The message is for a server side function built with the
      expression API. See :std:ref:`server-side-functions-api`.

      Throws an :type:`RPCException` if there is no method of that name taking the arguments.

.. type:: class Stream<T>

   A stream. See :std:ref:`java-client-streams`.

   Two stream objects are equal, and have the same ``hashCode``, when they refer to the same stream
   on the server. Streams order by their ids, so they can be sorted and used as the keys of a
   :type:`~java.util.TreeMap`.

   .. method:: T get()

      Returns the most recent value of the stream, waiting for the first value if none has arrived
      yet. Throws the exception that ended the stream, if it has ended, and a
      :type:`StreamException` if the thread is interrupted.

   .. method:: Object getCondition()

      The condition variable, notified with ``notifyAll`` when the value of the stream changes, and
      when the stream ends.

   .. method:: void waitForUpdate()

      Blocks until the value of the stream changes. Hold :meth:`getCondition()` when calling this
      method.

      Throws the exception that ended the stream, if it ends before or during the wait, and a
      :type:`StreamException` if the thread is interrupted. A thread holding the condition sees an
      ending as an update: the wait returns, and later reads and waits throw the exception.

   .. method:: boolean waitForUpdateWithTimeout(double timeout)

      Blocks until the value of the stream changes, or the timeout elapses. Returns ``true`` if the
      value changed, and ``false`` on a timeout. Hold :meth:`getCondition()` when calling this
      method.

      The timeout is in seconds, and ``Double.POSITIVE_INFINITY`` waits indefinitely. Throws an
      :type:`~java.lang.IllegalArgumentException` for a negative or NaN timeout. Throws the
      exception that ended the stream, if it ends before or during the wait, and a
      :type:`StreamException` if the thread is interrupted. A thread holding the condition sees an
      ending as an update: the wait returns ``true``, and later reads and waits throw the
      exception.

   .. method:: float getRate()
   .. method:: void setRate(float rate)

      The update rate of the stream, in Hertz. Zero, the default, means no limit.

   .. method:: int addCallback(java.util.function.Consumer<T> callback)

      Adds a callback, called with each new value of the stream, and returns a tag for it. If the
      stream already has a value, the callback is called with it straight away. The exception that
      ends a stream goes to its end callbacks.

   .. method:: void removeCallback(int tag)

      Removes the callback with the given tag.

   .. method:: int addEndCallback(java.util.function.Consumer<Exception> callback)

      Adds a callback, called once when the stream ends, with the exception that ended it, and
      returns a tag for it. It is called straight away if the stream has already ended.

   .. method:: void removeEndCallback(int tag)

      Removes the end callback with the given tag.

   .. method:: void remove()

      Removes the stream from the server. Other streams of the same call are not affected.

.. type:: class Event

   An event, made from a stream of :type:`~java.lang.Boolean` that is true when the event fires. See
   :std:ref:`java-client-events`.

   Two event objects are equal, and have the same ``hashCode``, when they refer to the same stream
   on the server. Events order as their streams do.

   .. method:: void waitFor()

      Blocks until the event fires. Returns straight away if the event has fired since the
      previous wait on this event object. Hold :meth:`getCondition()` when calling this method.

      Throws the exception that ended the event, if it ends before or during the wait, and a
      :type:`StreamException` if the thread is interrupted.

   .. method:: boolean waitForWithTimeout(double timeout)

      Blocks until the event fires, or the timeout elapses. Returns straight away if the event has
      fired since the previous wait on this event object. Returns ``true`` if the event fired, and
      ``false`` on a timeout. Hold :meth:`getCondition()` when calling this method.

      The timeout is in seconds, and ``Double.POSITIVE_INFINITY`` waits indefinitely. Throws an
      :type:`~java.lang.IllegalArgumentException` for a negative or NaN timeout. Throws the
      exception that ended the event, if it ends before or during the wait, and a
      :type:`StreamException` if the thread is interrupted.

   .. method:: Object getCondition()

      The condition variable, notified with ``notifyAll`` when the event fires, and when it ends.

   .. method:: int addCallback(java.lang.Runnable callback)

      Adds a callback, called each time the event fires, and returns a tag for it. It is called
      straight away for a firing that no wait has returned for and no callback has been called
      for.

   .. method:: void removeCallback(int tag)

      Removes the callback with the given tag.

   .. method:: int addEndCallback(java.util.function.Consumer<Exception> callback)

      Adds a callback, called once when the event ends, with the exception that ended it, and
      returns a tag for it. It is called straight away if the event has already ended.

   .. method:: void removeEndCallback(int tag)

      Removes the end callback with the given tag.

   .. method:: void remove()

      Removes the event from the server.

   .. method:: Stream<Boolean> getStream()

      The stream the event is made from.

.. type:: abstract class RemoteObject

   The base class of remote objects. Two remote objects are equal, and have the same
   ``hashCode``, when they refer to the same object on the server. Remote objects order by their
   ids, compared as unsigned integers.

   .. method:: static long getObjectId(RemoteObject obj)

      The identifier of a remote object on the server, e.g. to pass to
      :meth:`~krpc.client.services.KRPC.Expression.constantObject(Connection, long)`. Throws an
      :type:`~java.lang.IllegalArgumentException` if the object is ``null``.

.. type:: class ConnectionException extends java.io.IOException

   Thrown when connecting fails.

   .. method:: ConnectionResponse.Status getStatus()

      The status the server refused the connection with, such as ``DENIED``, or ``null``.

   .. method:: Disconnect.Reason getReason()

      The reason the server gave for ending the connection, such as ``SERVER_STOPPED``, or
      ``null``.

.. type:: class RPCException extends Exception

   Thrown for an error from the server that the client has no exception class for. The base class
   of the exception classes of the services.

.. type:: class StreamException extends Exception

   Thrown by a stream or event that you removed, when a stream cannot be created, and when a wait
   is interrupted.

.. type:: class EncodingException extends RuntimeException

   Thrown when a value cannot be encoded or decoded.

API Reference
-------------

.. toctree::
   :maxdepth: 1

   java/api/krpc
   java/api/stdlib
   java/api/space-center
   java/api/drawing
   java/api/ui
   java/api/infernal-robotics
   java/api/kerbal-alarm-clock
   java/api/remote-tech
   java/api/lidar
   java/api/docking-camera
   java/api/debug
