Client Design
=============

How the kRPC clients are built, and why. If you are writing a client, these pages are a starting
point for your own design. The behavior a client must have is in the :doc:`client specification
</client-spec>`, and the conformance tests enforce it.

This page covers the design the clients share. Each client's page covers its own decisions, and
lists where it departs from this page.

A client's departures from the specification, and the reason for each, are in two files under
``client/<client>/conformance/``. ``unsupported.txt`` lists what the language cannot do, and
``expected-failures.txt`` lists known failures.

Clients
-------

.. list-table::
   :header-rows: 1

   * - Client
     - Concurrency
     - Streams and events
     - Service stubs
   * - :doc:`Python <client-design/python>`
     - Threaded
     - Yes
     - Dynamic, with generated declarations
   * - :doc:`C# <client-design/csharp>`
     - Threaded
     - Yes
     - Pre-generated
   * - :doc:`C++ <client-design/cpp>`
     - Threaded
     - Yes
     - Pre-generated
   * - :doc:`C-nano <client-design/cnano>`
     - Single
     - No
     - Pre-generated
   * - :doc:`Java <client-design/java>`
     - Threaded
     - Yes
     - Pre-generated
   * - :doc:`Lua <client-design/lua>`
     - Single
     - No
     - Dynamic

Threaded Clients
----------------

Python, C#, C++ and Java share this design. A connection is one socket, and each call writes a
request to it. The server sends three kinds of message back on the same socket:

* A **response** answers one request, and carries the id of that request.
* A **stream update** carries new values for the connection's streams. A **stream** is a call the
  server repeats, so an update can arrive at any time, including between calls.
* A **disconnect** ends the connection, and carries the reason.

A program can also add **callbacks**, which are functions that run on each new value of a stream.
Each connection therefore runs two background threads, the **reader thread** and the **dispatcher
thread**.

Layers
^^^^^^

These clients divide their work into the same parts, with a class for each:

.. list-table::
   :header-rows: 1

   * - Layer
     - Work
     - Class
   * - Entry point
     - The object the program makes calls through
     - ``Connection``
   * - Framing
     - Splits the bytes on the socket into messages, each prefixed with its length
     - ``Transport`` in C++ and Python, ``MessageReader`` in C# and Java
   * - Session
     - Matches responses to calls, chooses the thread that reads, and owns the threads and the close
     - ``Session``
   * - Stream manager
     - Holds the connection's streams, and applies stream updates to them
     - ``StreamManager``
   * - Stream
     - Holds one stream's latest value, callbacks and condition
     - ``StreamImpl``
   * - Handles
     - The objects the program holds for a stream
     - ``Stream``, ``Event``

Threads
^^^^^^^

One thread at a time reads the socket. This job is the **reader role**, and each thread below can
hold it. The **role's holder** reads each message and handles it, whichever call or stream it is
for. A response to another thread's call goes to that call, and wakes its thread.

.. list-table::
   :header-rows: 1

   * - Thread
     - Work
   * - Caller
     - A program thread making a call. It writes its request and waits for the response, taking the
       role when no other thread holds it
   * - Reader thread
     - Takes the role and reads while streams are open and no call is waiting for a response
   * - Dispatcher thread
     - Runs callbacks, wakes threads waiting on a stream, and makes the writes queued by the role's
       holder

This model has three aims. A call from a single thread stays fast, as it hands off to no other
thread. Stream updates keep arriving between calls. A callback can make calls and take its time, as
the thread running it does not need to keep reading the socket.

Four rules shape how these threads work together:

* **Callers read their own responses.** This is faster than a hand-off to the reader thread.
* **The reader thread waits for data without the role.** A caller then never queues behind it. With
  no streams open, the reader thread sleeps and the socket has no reader between calls. The next
  call finds any ``disconnect`` that arrived.
* **A thread handling a message makes no writes.** It queues them for the dispatcher thread. A
  caller holds the write lock for a whole write, which blocks while the send buffer is full. A
  thread waiting on that lock would stop reading the server's messages.
* **The role's holder runs no user code, and takes no lock user code can hold.** A call made from a
  callback waits for the role's holder to read its response. If the holder ran that callback, the
  call would never finish.

Calls
^^^^^

Each call has a **pending slot**, which is the record of a call waiting for its response. The
session keeps the slots by request id:

1. The caller adds its slot, before the write, so a response arriving at once finds it.
2. The caller writes its request. The write lock is held for one whole message, so messages from
   different threads never interleave.
3. The caller waits, holding no lock, so other threads can make calls. The role's holder puts the
   response in the slot.

A caller that stops waiting, such as on an interrupt, abandons its slot. The response is discarded
when it arrives. If the call added a stream, that stream is removed, as no handle holds it.

Stream Values
^^^^^^^^^^^^^

A program can read a stream's value, or wait for its next value using the stream's condition. A
**condition** is a lock with a way to wake the threads waiting on it. A new value takes two steps:

1. The role's holder stores the value, and queues the rest for the dispatcher thread. Messages are
   handled in the order they arrive. A returning call therefore sees values at least as new as the
   server state before it.
2. The dispatcher thread **announces** the value, which wakes the threads waiting on the condition,
   and runs the callbacks. Callbacks can therefore lag behind the stored values.

A thread holding a stream's condition reads the announced value, not the stored one. The value it
reads stays the same until it waits.

An event is a stream of a bool, and each true value is a **firing**. The stream counts its firings,
and each event object keeps the count its last wait saw. A wait returns straight away if the event
has fired since then.

A remove or a close discards an unseen firing, so the wait raises an error. When the server ends the
stream, a wait returns an unseen firing before it raises the error.

A stream's first update can arrive straight after the response to the call that added it. A call
returning a stream or an event therefore marks its slot, and the role's holder registers the stream
before it reads the next message. The first update then always finds the stream. It stays undecoded
until a handle gives the value's type.

Callbacks
^^^^^^^^^

Callbacks normally run on the dispatcher thread, one at a time. The other cases are the first run
of a new callback, and **end callbacks**, which run once when a stream ends:

.. list-table::
   :header-rows: 1

   * - Callback
     - Thread
   * - Value, event or stream update
     - Dispatcher thread
   * - First run of a callback added to a stream that has a value, and the values queued during it
     - Adding thread
   * - End callback, from a remove or a close
     - Thread calling the remove or the close
   * - End callback, from the server ending the stream or a connection failure
     - Dispatcher thread, or an adding thread during a first run
   * - End callback, added after the stream ended
     - Adding thread

Close and Failure
^^^^^^^^^^^^^^^^^

These events fail the connection:

* A read or a write fails.
* A message breaks the protocol.
* A message **stalls**, which means it stops arriving part way through.
* The server sends a ``disconnect``.

Calls waiting for a response then fail with that error, and every stream ends.

A close, from the program's ``close`` or equivalent, can run on any thread. This includes the
dispatcher thread, from a callback:

1. It sends a ``disconnect`` and closes the sending side of the socket. It skips this step if the
   write lock stays held past a fixed timeout.
2. It discards incoming data until the server closes the socket, or the same timeout passes.
3. It ends the streams, which wakes the threads waiting on them.
4. It waits for the reader and dispatcher threads to finish, except the one it runs on. It skips the
   dispatcher thread when the closing thread holds a stream's condition, as a callback can be
   waiting for that condition.

Single-Threaded Clients
-----------------------

A call writes its request, then reads until the response with its id. Only a call reads the socket.
These clients have no streams or events, as a stream update needs a thread to read it between calls.

The read loop skips a message case it does not know, and fails the call on a ``disconnect``. Lua and
C-nano treat any other message as a protocol error, including a ``stream_update``.

Stream and Call Forms
---------------------

A program adding a stream needs to name a remote procedure without calling it. The same applies when
it gets a call as a value, such as to use in an expression. The clients do this in two ways:

* Python, C# and Java have one method for each purpose, ``add_stream`` and ``get_call`` or their
  equivalents. It takes any property or method, and works out the procedure at run time. Lua has
  only ``get_call``, as it has no streams.
* C++ and C-nano cannot look up a method at run time. Their generated stubs instead have extra
  functions for each procedure. C++ adds ones ending in ``_stream`` and ``_call``. C-nano has no
  streams, so it adds only ``_call``.

For the ``MeanAltitude`` property of a ``Flight`` object:

.. list-table::
   :header-rows: 1

   * - Client
     - Stream
     - Procedure call
   * - Python
     - ``conn.add_stream(getattr, flight, "mean_altitude")``
     - ``conn.get_call(getattr, flight, "mean_altitude")``
   * - C#
     - ``connection.AddStream (() => flight.MeanAltitude)``
     - ``Connection.GetCall (() => flight.MeanAltitude)``
   * - C++
     - ``flight.mean_altitude_stream()``
     - ``flight.mean_altitude_call()``
   * - C-nano
     - None
     - ``krpc_SpaceCenter_Flight_MeanAltitude_call(&call, flight)``
   * - Java
     - ``connection.addStream(flight, "getMeanAltitude")``
     - ``connection.getCall(flight, "getMeanAltitude")``
   * - Lua
     - None
     - ``conn:get_call(flight.get_mean_altitude, flight)``

In Python, C# and Java, the stream runs on the connection the method is called on, whichever
connection the object came from. In C++, it runs on the object's client, or for a static method the
client passed to it.

Static Methods
--------------

A static method takes its connection from one place, the one its language gives a class:

* In Python and Lua a class is a value. Each connection builds its own, and a static method runs
  on the connection of the service the class is reached through.
* In C#, Java and C++ a class is a type. The connection is the static method's first argument. In
  C-nano, every function takes the connection first.

A stream or call message of a static method never takes the connection a second time. In C# and
Java, a stream runs on the connection its ``AddStream`` is called on, and the connection named in
the call is not used. A call message holds no connection.

C#, Java and C++ also generate ``Bind`` on each class with static methods. It returns a ``Bound``
object holding the connection, with each static method minus that parameter. It is the counterpart
of ``conn.krpc.Expression`` in Python, and shortens an expression tree that passes the connection
at every node. The ``Bound`` methods carry the RPC attribute of the static method, so a C# lambda
or a Java ``addStream`` takes them as it takes a service's methods. A C++ ``Bound`` is a shorthand
for the calls that build large expression trees. It has no ``_stream`` or ``_call`` forms, which
stay on the static method.

.. toctree::
   :hidden:

   client-design/python
   client-design/csharp
   client-design/cpp
   client-design/cnano
   client-design/java
   client-design/lua

Deprecation
-----------

Each client signals the use of a deprecated member with the language's own mechanism:

.. list-table::
   :header-rows: 1

   * - Client
     - Signal
     - Kinds with no signal
   * - Python
     - ``DeprecationWarning`` at run time, and ``@deprecated`` for type checkers
     -
   * - C#
     - ``[Obsolete]``
     -
   * - C++
     - ``[[deprecated]]``
     -
   * - C-nano
     - ``KRPC_DEPRECATED``
     - Services and exceptions, which have no C type
   * - Java
     - ``@Deprecated``, which holds no reason
     - Streams and calls, which name their method in a string
   * - Lua
     - A warning on standard error, once per member on a connection
     - Exceptions, which the client does not expose, and structure fields

A stream, and the call a server side function makes, use a procedure too. Python and Lua warn when
the client builds the call. In C++ and C-nano, the ``_stream`` and ``_call`` functions carry the
attribute. In C#, the lambda that names the procedure warns.

A member of a deprecated service, class, enumeration or structure is deprecated. A member with no
reason of its own takes the reason of its nearest deprecated container. ``clientgen`` and the Python
and Lua clients apply this to their copy of the definitions when they read them. The protocol and
``docgen`` keep the attributes as declared, so the API reference has one notice per page.

A deprecated service is marked too: the C# extension method and service class, and the C++, Java
and Python service class. The Python and Lua clients keep it out of the client's attributes, so
that reading it reaches the attribute-miss hook, which warns.

Generated C++ and C-nano code uses the deprecated declarations it makes, so each header suppresses
the warning around its own contents. The suppression goes by the location of the use, so a use in a
program still warns.
