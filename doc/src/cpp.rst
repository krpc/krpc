.. default-domain:: cpp
.. highlight:: cpp

C++
===

This guide describes how to install the C++ client, and the features it provides. The
:ref:`cpp-client-reference` describes each function and class.

Installing the Library
----------------------

vcpkg
^^^^^

The C++ client is available from `vcpkg <https://vcpkg.io>`_, which builds it along with its
`ASIO <http://think-async.com/>`_ and `protobuf <https://github.com/google/protobuf>`_
dependencies. To install it:

.. tabs::

   .. tab:: Linux

      .. code-block:: bash

         vcpkg install krpc

   .. tab:: Windows

      .. code-block:: bash

         vcpkg install krpc:x64-windows

A project in manifest mode lists the client in its ``vcpkg.json`` instead, and vcpkg installs it
when you configure the project:

.. code-block:: json

   {
     "dependencies": ["krpc"]
   }

CMake
^^^^^

The C++ client requires `ASIO <http://think-async.com/>`_ and `protobuf
<https://github.com/google/protobuf>`_. By default, CMake looks for these as system-installed
packages. On Ubuntu, install them with:

.. code-block:: bash

   apt-get install libasio-dev libprotobuf-dev protobuf-compiler

CMake 3.15 or later is required. `Download the source archive
<https://github.com/krpc/krpc/releases>`_, extract it and run:

.. code-block:: bash

   cmake -B build
   cmake --build build
   cmake --install build

Pass ``-DKRPC_FETCH_DEPS=ON`` when you configure, to download every dependency with FetchContent
in place of the system packages. The options ``-DKRPC_FETCH_PROTOBUF=ON``,
``-DKRPC_FETCH_ASIO=ON`` and ``-DKRPC_FETCH_ABSL=ON`` choose one dependency each. An option that
is ``OFF``, the default, requires the system package.

To install the client in a directory of your choice, pass ``CMAKE_INSTALL_PREFIX`` when you
configure it:

.. code-block:: bash

   cmake -B build -DCMAKE_INSTALL_PREFIX=/path/to/install
   cmake --build build
   cmake --install build

Compiling a Program
-------------------

CMake
^^^^^

Find the installed library, and link your program against it, in your ``CMakeLists.txt``:

.. code-block:: cmake

   set(CMAKE_CXX_STANDARD 17)
   find_package(krpc CONFIG REQUIRED)
   target_link_libraries(my_app PRIVATE krpc::krpc)

For a client installed with vcpkg, pass the vcpkg toolchain file when you configure your project:

.. code-block:: bash

   cmake -B build -DCMAKE_TOOLCHAIN_FILE=/path/to/vcpkg/scripts/buildsystems/vcpkg.cmake

For a client installed in a directory of your choice, pass that directory as
``CMAKE_PREFIX_PATH``, so that ``find_package`` finds the client there:

.. code-block:: bash

   cmake -B build -DCMAKE_PREFIX_PATH=/path/to/install

Compiler Command Line
^^^^^^^^^^^^^^^^^^^^^

Link your program against ``libkrpc``, ``libprotobuf-lite`` and ``libz``. For a program saved as
``main.cpp``:

.. tabs::

   .. tab:: GCC

      .. code-block:: bash

         g++ main.cpp -std=c++17 -lkrpc -lprotobuf-lite -lz

   .. tab:: Clang

      .. code-block:: bash

         clang++ main.cpp -std=c++17 -lkrpc -lprotobuf-lite -lz

Connecting to the Server
------------------------

The game must be running with the server started. :ref:`getting-started-server` in the
getting started guide shows how to install and start it.

The client connects over TCP/IP or over a local socket. Each server in the server window uses one
protocol, and accepts connections over that protocol only. Pick the protocol when you add the
server, and connect with the matching function.

.. note::

   A server with "Auto-accept new clients" turned off answers only once the player approves the
   connection in game. The connection fails after 5 seconds by default. Pass a ``handshake_timeout``
   of zero to wait indefinitely for the player.

Over TCP/IP
^^^^^^^^^^^

Select "Protobuf over TCP/IP" as the server's protocol. :func:`~krpc::connect` connects to it, and
returns a :class:`~krpc::Client`. You make every call through this object. The following connects
to a server with the default settings, address ``127.0.0.1`` and port 50000, and prints its
version:

.. literalinclude:: /scripts/cpp/Connecting1.cpp

:func:`~krpc::connect` also takes a name for the connection, and the address and port of the
server. The address and port match the ones shown in the server window. The name appears in the
server window in the game:

.. literalinclude:: /scripts/cpp/Connecting2.cpp

TCP/IP works over a network, so your program can run on a different machine from the game.

Over a Local Socket
^^^^^^^^^^^^^^^^^^^

Select "Protobuf over local socket" as the server's protocol. :func:`~krpc::connect_local`
connects to it, and returns a :class:`~krpc::Client` as :func:`~krpc::connect` does. The following
connects to a server with the default settings, at the default socket path for your platform, and
prints its version:

.. literalinclude:: /scripts/cpp/ConnectingLocal.cpp

:func:`~krpc::connect_local` also takes a ``path``, for a server with another socket path. The
path matches the one shown in the server window.

A local socket works only on the machine running the game. It has lower overhead than TCP/IP, so
it is faster.

Closing the Connection
^^^^^^^^^^^^^^^^^^^^^^

A :class:`~krpc::Client` can be copied, and every copy shares the connection. The connection
closes when the last copy is destroyed, such as at the end of the block that declares it:

.. literalinclude:: /scripts/cpp/Connecting3.cpp

Calling Remote Procedures
-------------------------

The server provides *procedures* for your program to call, grouped into *services*.

The following gets the active vessel from :func:`~krpc::services::SpaceCenter::active_vessel`, an
object of type :class:`~krpc::services::SpaceCenter::Vessel`. It sets the vessel's name, then prints
its altitude:

.. literalinclude:: /scripts/cpp/RemoteProcedures.cpp

Each service is a class in the ``krpc::services`` namespace, declared in a header of its own, such
as ``krpc/services/space_center.hpp`` for the :class:`~krpc::services::SpaceCenter` service. Include
the header, and construct the service with a pointer to the connection.

Handling Errors
---------------

A procedure that fails on the server throws an exception in your program. The following catches
the error from loading a save that does not exist:

.. literalinclude:: /scripts/cpp/Errors.cpp

The exception you catch depends on the error:

* A standard error, such as an invalid argument, throws a subclass of the matching standard
  exception. The example catches ``std::invalid_argument``, as a missing save throws
  :class:`~krpc::services::KRPC::ArgumentException`.
* An error particular to a service throws a class of that service, such as
  :class:`~krpc::services::KRPC::ObjectDestroyedException`.
* An error the client has no class for throws :class:`~krpc::RPCError`.
* A call made once the connection has failed throws :class:`~krpc::ConnectionError`.

Remote Objects
--------------

Many procedures return an object identifying something in the game, such as a vessel or a part.
The object holds no copy of the game's state. It is just a reference to it. Each call through it
reads the game as it is now:

.. literalinclude:: /scripts/cpp/RemoteObjects.cpp

Two objects for the same thing in the game compare equal. Objects are also ordered, so they can be
used as ``std::map`` keys, and :class:`~krpc::hash` hashes them for an unordered container.

Once the thing an object identifies is destroyed, such as a part that explodes, each call through
it throws :class:`~krpc::services::KRPC::ObjectDestroyedException`.

An object for a vessel or a part keeps working when you load a save, quickload or revert, and
then reads the loaded game. Other objects depend on what they identify. A maneuver node, for
example, is destroyed by a load.

A vessel far from the active vessel is *unloaded*, and the game keeps no parts for it. The vessel
itself reads normally, but a call on one of its parts throws
:class:`~krpc::services::KRPC::ObjectNotLoadedException`. The part works again once the vessel comes
back into range.

.. seealso:: :doc:`/user-guide/object-lifetime` describes how each kind of object behaves in more
             detail.

.. _cpp-client-streams:

Streaming Data
--------------

A common use of kRPC is to read data from the game continuously. The following does so by calling
a remote procedure in a loop, printing the position of the active vessel:

.. literalinclude:: /scripts/cpp/Streaming1.cpp

Each call sends a request to the server and waits for its response. A *stream* is more efficient.
The server runs the call repeatedly, and sends your program the result each time it changes.

The following does the same as above using a stream:

.. literalinclude:: /scripts/cpp/Streaming2.cpp

Each procedure has a function with ``_stream`` added to its name, such as ``position_stream``. It
creates the stream from the procedure's arguments, and returns a :class:`~krpc::Stream`, whose
template argument is the procedure's return type. Calling the stream returns its latest value.

A property is streamed the same way, with ``_stream`` added to the name of its getter:

.. literalinclude:: /scripts/cpp/Streaming3.cpp

A stream can be made of any procedure or property that returns a value. Each call to a
``_stream`` function creates another stream.

A stream runs until you remove it with :func:`~krpc::Stream::remove`, or the connection closes.
A stream object can be copied, and every copy refers to the same stream. Destroying the last copy
also removes the stream, so a stream declared in a block is removed at the end of the block:

.. literalinclude:: /scripts/cpp/StreamScope.cpp

By default, the server sends a stream's value each time it changes. Call
:func:`~krpc::Stream::set_rate` to limit how many times a second the stream updates:

.. literalinclude:: /scripts/cpp/StreamRate.cpp

Waiting for Changes
^^^^^^^^^^^^^^^^^^^

Each stream has a condition variable, a :class:`~krpc::Condition`, which is used like a
``std::condition_variable``. :func:`~krpc::Stream::wait` blocks until the stream's value changes.
The following waits for the abort button to be pressed:

.. literalinclude:: /scripts/cpp/ConditionVariables.cpp

Hold the condition when you call :func:`~krpc::Stream::wait`, as :func:`~krpc::Stream::acquire`
does here, and :func:`~krpc::Stream::release` lets go of it. While you hold it, the value changes
only when you wait, so the loop misses no updates. An ending reaches you the same way: the wait
returns, and your next read or wait throws the exception that ended the stream.

:func:`~krpc::Stream::wait` takes a timeout in seconds, and returns ``false`` when it elapses
before an update.

Callbacks
^^^^^^^^^

A callback is a function the client calls with each new value of a stream. The following adds
two callbacks to a stream of the abort button:

.. literalinclude:: /scripts/cpp/Callbacks.cpp

A callback added to a stream that already has a value is called with that value straight away.
It is then called with each new value, in order.

.. note::

   Each connection has a callback thread, which runs the callbacks of its streams one at a time. A
   callback can make remote procedure calls. Other callbacks and waits are held up while it runs, so
   return promptly, and do not wait on a stream in a callback. For a long running task, start a
   worker thread from the callback.

.. _cpp-client-stream-endings:

Stream Endings
^^^^^^^^^^^^^^

A stream ends when its procedure throws an exception, when the server removes it, when you remove
it, or when the connection closes. Calling the stream or waiting on it then throws the exception
that ended it. The following streams the temperature of a part until the part is destroyed:

.. literalinclude:: /scripts/cpp/StreamEnd.cpp

The exception is the one the procedure threw, or one of the following:

* :class:`~krpc::services::KRPC::StreamRemovedException` when the server removes the stream, such as
  for an :ref:`event <cpp-client-events>` that has finished.
* :class:`~krpc::StreamError` when you remove the stream from the client.
* :class:`~krpc::ConnectionError` when the connection closes or fails.

An exception for an object that is not loaded, or for a procedure not available in the current
game scene, does not end the stream. It pauses the stream's updates, and the stream keeps its last
value. The updates resume once the object or the scene is available.

.. seealso:: :doc:`/user-guide/object-lifetime` describes when each kind of object is not loaded,
             and how streams behave around a load.

:func:`~krpc::Stream::add_end_callback` adds a callback that is called once when the stream
ends, with the exception as a ``std::exception_ptr``. The following prints the temperature of a
part with a callback, and prints the exception when the stream ends:

.. literalinclude:: /scripts/cpp/EndCallback.cpp

Server Side Functions
---------------------

A server side function runs inside the game. You build it in your program with the expression
API, and the client sends it to the server, which runs it there. The following reads the vessel's
altitude and speed together:

.. literalinclude:: /scripts/cpp/RunFunction.cpp

Each procedure has a function with ``_call`` added to its name, such as ``mean_altitude_call``. It
returns the call as a message, for :func:`~krpc::services::KRPC::Expression::call` to embed in the
function. :ref:`server-side-functions-api` in the :ref:`server side functions guide
<server-side-functions>` describes how to build functions.

:func:`~krpc::run`, :func:`~krpc::add_stream` and :func:`~krpc::add_event` are declared in
``krpc/function_stream.hpp``.

Running a Function
^^^^^^^^^^^^^^^^^^

:func:`~krpc::run` runs a function once, and returns its value. The template argument gives the
type of the value. The server runs the whole function within one physics tick, so every value it
reads comes from the same moment in the game. In the example, the altitude and the speed are
consistent with each other, which two separate calls cannot guarantee.

Streaming a Function
^^^^^^^^^^^^^^^^^^^^

Pass a function to :func:`~krpc::add_stream` to stream its value. The template argument gives the
type of the value. The server evaluates the function on each update, within one physics tick. The
following streams the vessel's altitude in kilometers:

.. literalinclude:: /scripts/cpp/FunctionStream.cpp

A function stream ends on an exception its function throws, just like an
:ref:`ordinary stream <cpp-client-stream-endings>`. Unlike an ordinary stream, it also ends on
:class:`~krpc::services::KRPC::ObjectNotLoadedException`, thrown while the game loads or when a
vessel unloads. The server cannot pause a function partway through, and running it again would
repeat its effects. To keep a function running through a load, catch the exception in the
function:

.. literalinclude:: /scripts/cpp/NotLoadedStream.cpp
   :language: cpp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

.. _cpp-client-events:

Events
------

A common use of kRPC is to wait for something to happen in the game, such as the vessel reaching
an altitude. An *event* does this efficiently. The server checks a condition, written as a server
side function, and tells your program when it becomes true. The following waits until the
vessel's altitude exceeds 1000 meters:

.. literalinclude:: /scripts/cpp/Event.cpp

:func:`~krpc::add_event` creates the event, and :func:`~krpc::Event::wait` waits for it to fire.
The server checks the condition every physics tick, so the event catches a condition that holds
for only a moment, which a loop of calls can miss.

The event fires again each time the condition becomes true after being false. Call
:func:`~krpc::Event::remove` to stop it.

As with a stream, hold the event's condition when you call :func:`~krpc::Event::wait`. A wait
returns straight away if the event has fired since the last wait on the same event object, so a
firing between two waits is not missed.

Event Callbacks
^^^^^^^^^^^^^^^

An event can also call a callback each time it fires. The callback takes no arguments:

.. literalinclude:: /scripts/cpp/EventCallback.cpp

Callbacks for events run on the connection's callback thread, as the callbacks of a stream do.

Event Endings
^^^^^^^^^^^^^

An event ends in the same ways as a :ref:`stream <cpp-client-stream-endings>`. Waiting on an
event that has ended throws the exception that ended it.

When the event fires just before the server ends it, your next wait returns for that firing first.
Removing the event, or destroying its last copy, ends it at once.

:func:`~krpc::Event::add_end_callback` adds a callback that is called once when the event ends,
with the exception, just like an ordinary stream.

Threading
---------

A connection can be shared between threads. Each thread makes calls over it as it would alone,
and each call returns its own result. The following prints the altitude and speed of the vessel
from two threads:

.. literalinclude:: /scripts/cpp/Threads.cpp

Streams and events can also be used from any thread:

* Several threads can read a stream, or wait on a stream or an event. A change wakes every thread
  waiting for it. Each thread waits through its own copy of the stream or event object.
* A thread holding a stream's condition does not stop other threads from making calls.
* Callbacks run on the connection's callback thread, one at a time, and never on your program's
  threads. A callback added to a stream that already has a value is different. Its first call, with
  that value, might run on your thread, before :func:`~krpc::Stream::add_callback` returns.
  Protect state that a callback shares with your program's threads, such as with a
  ``std::mutex``.

When the connection fails, the calls and waits of every thread end with
:class:`~krpc::ConnectionError`. A wait on a stream or an event that another thread removes ends
with the exception for a removed stream.

Deprecated Members
------------------

A member of a service can be deprecated, when a newer member replaces it. A deprecated member
still works, but may be removed in a future release, so move to its replacement.

The API reference and the member's documentation comment mark it as deprecated, and name the
replacement. The member is also declared ``[[deprecated]]`` with that message, so the compiler
warns where your program uses it.

Service Stubs
-------------

The client includes code for each service that ships with kRPC, generated from the services of
one kRPC release. These *stubs* are the headers in ``krpc/services``, such as
``krpc/services/space_center.hpp``.

The stubs match the server from the same release, so install the client version that matches your
server. With another version, a procedure that was removed or renamed throws an exception when
you call it. A parameter or return type that changed is not checked, so a call can send or return
a wrong value with no error.

A service with no stub, such as one added by another mod, needs stubs of its own.
:ref:`service-api-clientgen` shows how to generate them.

.. _cpp-client-reference:

Client API Reference
--------------------

.. namespace:: krpc

.. function:: Client connect(const std::string& name = "", const std::string& address = "127.0.0.1", unsigned int port = 50000, std::chrono::milliseconds timeout = std::chrono::milliseconds::zero(), std::chrono::milliseconds handshake_timeout = std::chrono::seconds(5), std::chrono::milliseconds stall_timeout = std::chrono::seconds(30))

   Connects to a kRPC server, and returns a :class:`~krpc::Client`.

   :parameters:

      * **name** (*std::string*) -- A name for the connection, shown in the server window in the
        game.
      * **address** (*std::string*) -- The address of the server, as a host name or an IP
        address. Defaults to ``"127.0.0.1"``.
      * **port** (*unsigned int*) -- The port of the server. Defaults to 50000.
      * **timeout** (*std::chrono::milliseconds*) -- How long to wait for the connection to open.
        Defaults to zero, which waits indefinitely. Without a timeout, a network that drops the
        connection attempt leaves the client waiting.
      * **handshake_timeout** (*std::chrono::milliseconds*) -- How long to wait for the server to
        accept the connection. Defaults to 5 seconds. Zero waits indefinitely, such as for a
        server that asks the player to approve each connection.
      * **stall_timeout** (*std::chrono::milliseconds*) -- How long a message from the server can
        go without a new byte, once it has started to arrive, before the connection fails.
        Defaults to 30 seconds. Zero waits indefinitely.

   Throws :class:`~krpc::ConnectionError` if the connection fails or the server refuses it, and
   ``std::invalid_argument`` for a negative handshake or stall timeout.

.. function:: Client connect_local(const std::string& name = "", const std::string& path = "", std::chrono::milliseconds handshake_timeout = std::chrono::seconds(5), std::chrono::milliseconds stall_timeout = std::chrono::seconds(30))

   Connects to a kRPC server on the same machine over a local socket, and returns a
   :class:`~krpc::Client`. The server must use the local socket protocol.

   Local sockets are available on Linux, macOS, and Windows 10 1803 and Windows Server 2019 or
   later.

   :parameters:

      * **name** (*std::string*) -- A name for the connection, shown in the server window in the
        game.
      * **path** (*std::string*) -- The path of the server's socket, shown in the server window in
        the game. An empty path, the default, is the path the server uses unless it was
        configured with another.
      * **handshake_timeout** (*std::chrono::milliseconds*) -- As for :func:`~krpc::connect`.
      * **stall_timeout** (*std::chrono::milliseconds*) -- As for :func:`~krpc::connect`.

   Throws :class:`~krpc::ConnectionError` and ``std::invalid_argument`` as :func:`~krpc::connect`
   does.

.. class:: Client

   A connection to the server. Obtain one by calling :func:`~krpc::connect`. A service is
   constructed with a pointer to it, such as ``krpc::services::SpaceCenter sc(&conn)``.

   Client objects are copy constructible and assignable, and every copy shares the connection.
   Destroying the last copy closes the connection. A default-constructed client is not
   connected. Its calls and its stream update members throw :class:`~krpc::StreamError`.

   .. function:: void wait_for_stream_update(double timeout = -1)

      Blocks until the next stream update message is processed, or the timeout elapses. A timeout
      of zero or more is in seconds, and a negative one waits indefinitely. Hold the stream update
      condition, with :func:`acquire_stream_update`, when calling this method.

      Returns straight away once the connection has failed. It can also return before an update,
      so check for the change you are waiting for.

   .. function:: void acquire_stream_update()

      Locks the mutex of the stream update condition variable.

   .. function:: void release_stream_update()

      Unlocks the mutex of the stream update condition variable.

   .. function:: Condition& get_stream_update_condition() const

      A condition variable notified after each stream update message is processed.

   .. function:: std::unique_lock<RecursiveMutex>& get_stream_update_condition_lock() const

      A lock on the re-entrant mutex of the stream update condition variable. Every copy of the
      client shares the lock, so one thread at a time uses it.

   .. function:: int add_stream_update_callback(const std::function<void()>& callback)

      Adds a callback, with no arguments, called after each stream update message is processed.
      It runs on the connection's callback thread, as the callbacks of a stream do. Returns a tag
      that identifies the callback.

   .. function:: void remove_stream_update_callback(int tag)

      Removes a stream update callback, given the tag :func:`add_stream_update_callback`
      returned.

.. class:: template <typename T> Object

   The base class of remote objects. Two objects compare equal with ``==`` when they identify
   the same thing in the game. ``<`` and the other comparisons order objects by their
   identifiers, and ``<<`` writes an object's class name and identifier to a stream.

.. function:: template <typename T> uint64_t get_object_id(const Object<T>& object)

   The identifier of a remote object on the server, e.g. to pass to
   :func:`~krpc::services::KRPC::Expression::constant_object`. Throws ``std::invalid_argument`` for
   a null object, whose identifier is 0.

.. class:: template <typename T> Stream

   A stream, created by a procedure's ``_stream`` function. See :ref:`cpp-client-streams`.

   Stream objects are copy constructible and assignable, and every copy refers to the same stream.
   Call :func:`remove`, or destroy every copy, to remove the stream from the server. A
   default-constructed stream object refers to no stream. Its members throw
   :class:`~krpc::StreamError`, except :func:`remove`, which does nothing, and the comparisons.

   .. function:: T operator()()

      Returns the most recent value of the stream, waiting for the first value if none has arrived
      yet. Throws the exception that ended the stream, if it has ended.

   .. function:: bool wait(double timeout = std::numeric_limits<double>::infinity())

      Blocks until the value of the stream changes, or the timeout elapses. Returns ``true`` if
      the value changed, and ``false`` on a timeout. Hold the condition, with :func:`acquire`,
      when calling this method.

      The timeout is in seconds, and the default waits indefinitely. Throws
      ``std::invalid_argument`` for a negative or NaN timeout. Throws the exception that ended the
      stream, if it ends before or during the wait. A thread holding the condition sees an ending as
      an update: the wait returns ``true``, and later reads and waits throw the exception.

   .. function:: void acquire()

      Locks the mutex of the stream's condition variable. Every copy of the stream object shares
      the mutex, so a lock taken through one copy can be released through another.

   .. function:: void release()

      Unlocks the mutex of the stream's condition variable.

   .. function:: Condition& get_condition() const

      A condition variable notified when the value of the stream changes. Every copy of the
      stream object shares it.

   .. function:: std::unique_lock<RecursiveMutex>& get_condition_lock() const

      A lock on the re-entrant mutex of the condition variable. Each stream object has its own
      lock, used by one thread, so a thread waiting on a stream uses its own copy of the stream
      object.

   .. function:: float rate()
                 void set_rate(float rate)

      The update rate of the stream, in Hertz. Zero, the default, means no limit.

   .. function:: int add_callback(const std::function<void(T)>& callback)

      Adds a callback, called with each new value of the stream. If the stream already has a
      value, the callback is called with it straight away. The exception that ends a stream goes
      to its end callbacks. Returns a tag that identifies the callback.

   .. function:: void remove_callback(int tag)

      Removes a callback, given the tag :func:`add_callback` returned.

   .. function:: int add_end_callback(const std::function<void(std::exception_ptr)>& callback)

      Adds a callback, called once when the stream ends, with the exception that ended it. It is
      called straight away if the stream has already ended. Returns a tag that identifies the
      callback.

   .. function:: void remove_end_callback(int tag)

      Removes an end callback, given the tag :func:`add_end_callback` returned.

   .. function:: void remove()

      Removes the stream from the server, for every copy of the stream object. Other streams of
      the same call are not affected.

   .. function:: bool operator==(const Stream<T>& rhs) const
                 bool operator!=(const Stream<T>& rhs) const

      Whether the two stream objects refer to the same stream. Two streams created for the same
      call are not equal.

   .. function:: explicit operator bool() const

      Whether the stream object refers to a stream.

.. function:: template <typename T = void, typename... Args> T run(const services::KRPC::Expression& function, const Args&... args)

   Runs a server side function within a single physics tick, and returns its value. With the
   ``void`` template type, the function is run for its effects. The arguments are passed to the
   function's parameters, with ``std::nullopt`` or ``nullptr`` for a null.

   A number converts to a ``double`` parameter, and an integer or a ``float`` to a ``float``
   parameter, as C++ converts it implicitly, which can round. An integer converts to a signed
   parameter that holds every value of its type, and an integer that is not negative to an
   unsigned parameter at least as wide. A number a collection or a tuple holds converts the same
   way.

   Throws ``std::invalid_argument`` if the template type does not describe the function's value,
   the number of arguments differs from the number of parameters, or an argument does not match
   its parameter. Throws :class:`~krpc::EncodingError` for a null value unless the template type
   is a ``std::optional``.

   A class, enumeration or structure argument is checked by kind only. The server checks its
   class, and a null for a parameter that is not nullable, and throws
   :class:`~krpc::services::KRPC::ArgumentException` for a mismatch.

.. function:: template <typename T> Stream<T> add_stream(const services::KRPC::Expression& function)

   Creates a stream from a server side function, evaluated on each update. Each call creates a
   new stream. A function that can produce a null needs a ``std::optional`` template type, and
   reading a null without one throws :class:`~krpc::EncodingError`. A callback is not called for
   a null value.

   Throws ``std::invalid_argument`` if the template type does not describe the function's value,
   :class:`~krpc::StreamError` if the client is not connected to a server, and
   :class:`~krpc::services::KRPC::ArgumentException` if the function has parameters.

.. function:: Event add_event(const services::KRPC::Expression& function)

   Creates an event from a server side function that evaluates to a boolean value. Each call creates
   a new event. Throws :class:`~krpc::StreamError` if the client is not connected to a server, and
   :class:`~krpc::services::KRPC::ArgumentException` if the function does not evaluate to a boolean
   value or has parameters.

.. class:: Event

   An event, made from a ``Stream<bool>`` that is true when the event fires. See
   :ref:`cpp-client-events`.

   Event objects are copy constructible and assignable, and every copy refers to the same event.
   Destroying the last copy removes the event from the server. A default-constructed event object
   refers to no event. Its members throw :class:`~krpc::StreamError`, except :func:`remove`,
   :func:`stream`, the comparisons and ``operator bool``.

   .. function:: bool wait(double timeout = std::numeric_limits<double>::infinity())

      Blocks until the event fires, or the timeout elapses. Returns straight away if the event
      has fired since the previous wait on this event object, or for a first wait, since the event
      was created. Returns ``true`` if the event
      fired, and ``false`` on a timeout. Hold the condition, with :func:`acquire`, when calling
      this method.

      The timeout is in seconds, and the default waits indefinitely. Throws
      ``std::invalid_argument`` for a negative or NaN timeout. Throws the exception that ended the
      event, if it ends before or during the wait.

   .. function:: void acquire()

      Locks the mutex of the event's condition variable. Every copy of the event object, and its
      stream, shares the mutex.

   .. function:: void release()

      Unlocks the mutex of the event's condition variable.

   .. function:: Condition& get_condition() const

      A condition variable notified when the event fires. Every copy of the event object shares
      it.

   .. function:: std::unique_lock<RecursiveMutex>& get_condition_lock() const

      A lock on the re-entrant mutex of the condition variable. Each event object has its own
      lock, used by one thread, so a thread waiting on an event uses its own copy of the event
      object.

   .. function:: int add_callback(const std::function<void()>& callback)

      Adds a callback, with no arguments, called each time the event fires. It is called straight
      away for a firing that no wait on this event object has returned for and no callback has
      been called for. Returns a tag that identifies the callback.

   .. function:: void remove_callback(int tag)

      Removes a callback, given the tag :func:`add_callback` returned.

   .. function:: int add_end_callback(const std::function<void(std::exception_ptr)>& callback)

      Adds a callback, called once when the event ends, with the exception that ended it. It is
      called straight away if the event has already ended. Returns a tag that identifies the
      callback.

   .. function:: void remove_end_callback(int tag)

      Removes an end callback, given the tag :func:`add_end_callback` returned.

   .. function:: void remove()

      Removes the event from the server.

   .. function:: Stream<bool> stream() const

      The stream the event is made from.

   .. function:: bool operator==(const Event& rhs) const
                 bool operator!=(const Event& rhs) const

      Whether the two event objects refer to the same event.

   .. function:: explicit operator bool() const

      Whether the event object refers to an event.

.. class:: ConnectionError

   Thrown when connecting fails, and by each call once the connection has failed. A subclass of
   ``std::runtime_error``.

   .. function:: const std::string& status() const

      The status the server refused the connection with, such as ``"DENIED"``, or empty.

   .. function:: const std::string& reason() const

      The reason the server gave for ending the connection, such as ``"SERVER_STOPPED"``, or
      empty.

.. class:: RPCError

   Thrown for an error from the server that the client has no exception class for. A subclass of
   ``std::runtime_error``, and the base class of each service exception with no standard
   equivalent.

.. class:: StreamError

   Thrown by a stream or event that you removed, and when a stream cannot be created. A subclass
   of ``std::runtime_error``.

.. class:: EncodingError

   Thrown when a value cannot be encoded or decoded. A subclass of ``std::runtime_error``.

.. class:: RecursiveMutex

   The mutex of a stream's, an event's or the stream update's condition variable. The thread
   holding it can lock it again, and each lock is matched by an unlock. It provides ``lock``,
   ``try_lock``, ``try_lock_for``, ``try_lock_until`` and ``unlock``, so it can be held with
   ``std::unique_lock`` and ``std::lock_guard``. ``unlock`` throws ``std::system_error`` on a
   thread that does not hold it.

   .. function:: bool held_by_this_thread() const

      Whether the calling thread holds the mutex.

.. class:: Condition

   The condition variable of a stream, an event or the stream update. Its ``wait``, ``wait_for``
   and ``wait_until`` take a ``std::unique_lock<RecursiveMutex>``, with or without a predicate,
   as ``std::condition_variable`` does. A wait releases every lock the calling thread holds on
   the mutex, and takes them again before it returns. Throws ``std::system_error`` if the calling
   thread does not hold the mutex. A wait can return without a notification, so call it in a loop
   that checks for the change.

   .. function:: void wait()
                 template <typename Rep, typename Period> std::cv_status wait_for(const std::chrono::duration<Rep, Period>& duration)
                 template <typename Clock, typename Duration> std::cv_status wait_until(const std::chrono::time_point<Clock, Duration>& deadline)

      Waits as the overloads taking a lock do, for a thread that holds the mutex through
      ``acquire`` or a lock of its own.

   .. function:: void notify_one()
                 void notify_all()

      Wakes one waiting thread, or every waiting thread.

.. function:: template <typename T> std::size_t hash_value(const T& value)

   Returns a hash of a value of any type the client carries: the arithmetic types, strings,
   enumerations, class objects, ``std::optional``, tuples, and the list, set and dictionary
   collection types, nested however deeply. A structure is also hashed by this function, and is
   given a ``std::hash`` of its own, so an unordered container can be keyed by one directly.

.. class:: hash

   A function object that calls :func:`hash_value`, for a standard container that takes its hash
   as a template argument. The standard library hashes neither ``std::tuple`` nor the collection
   types, so a container keyed by one needs this:

   .. code-block:: cpp

      std::unordered_set<std::tuple<double, double, double>, krpc::hash> points;

API Reference
-------------

.. toctree::
   :maxdepth: 1

   cpp/api/krpc
   cpp/api/stdlib
   cpp/api/space-center
   cpp/api/drawing
   cpp/api/ui
   cpp/api/infernal-robotics
   cpp/api/kerbal-alarm-clock
   cpp/api/remote-tech
   cpp/api/lidar
   cpp/api/docking-camera
   cpp/api/debug
