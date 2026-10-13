.. default-domain:: cpp
.. highlight:: cpp

C++ Client
==========

This client provides a C++ API for interacting with a kRPC server.

Installing
----------

vcpkg
^^^^^

The C++ client is available from `vcpkg <https://vcpkg.io>`_, which builds it along with
its `ASIO <http://think-async.com/>`_ (network communication) and `protobuf
<https://github.com/google/protobuf>`_ (message serialization) dependencies. It can be
installed as follows:

.. tabs::

   .. tab:: Linux

      .. code-block:: bash

         vcpkg install krpc

   .. tab:: Windows

      .. code-block:: bash

         vcpkg install krpc:x64-windows

Then integrate with your CMake project by passing the vcpkg toolchain file at configure time:

.. code-block:: bash

   cmake -B build -DCMAKE_TOOLCHAIN_FILE=/path/to/vcpkg/scripts/buildsystems/vcpkg.cmake

CMake
^^^^^

The C++ client requires `ASIO <http://think-async.com/>`_ (for network communication) and
`protobuf <https://github.com/google/protobuf>`_ (for message serialization). By default, CMake
looks for these as system-installed packages. On Ubuntu they can be installed using:

.. code-block:: bash

   apt-get install libasio-dev \
       libprotobuf-dev protobuf-compiler

CMake 3.15 or later is required.  `Download the source archive
<https://github.com/krpc/krpc/releases>`_, extract it and run:

.. code-block:: bash

   cmake -B build
   cmake --build build
   cmake --install build

To download all dependencies automatically via FetchContent instead of using system packages,
pass ``-DKRPC_FETCH_DEPS=ON`` to the CMake configure step. Fine-grained options
``-DKRPC_FETCH_PROTOBUF=ON``, ``-DKRPC_FETCH_ASIO=ON``, and ``-DKRPC_FETCH_ABSL=ON`` are also
available. When an option is ``OFF`` (the default) the system package is required.

Getting Started
---------------

The following example program connects to the server, queries it for its version and prints it out:

.. literalinclude:: /scripts/client/cpp/Basic.cpp

A kRPC program needs to be compiled with C++17 support enabled, and linked against ``libkrpc``,
``libprotobuf-lite`` and ``libz``.

To compile this example using GCC, save the source as ``main.cpp`` and run the
following:

.. code-block:: bash

   g++ main.cpp -std=c++17 -lkrpc -lprotobuf-lite -lz

If you build your program with CMake, link against the installed library in your ``CMakeLists.txt``:

.. code-block:: cmake

   find_package(krpc CONFIG REQUIRED)
   target_link_libraries(my_app PRIVATE krpc::krpc)

Connecting to the Server
^^^^^^^^^^^^^^^^^^^^^^^^^

The :func:`krpc::connect` function is used to open a connection to a server. It returns a client
object (of type :class:`krpc::Client`) through which you can interact with the server. When called
without any arguments, it will connect to the local machine on the default port numbers. You can
specify different connection settings, and also a descriptive name for the connection, as follows:

.. literalinclude:: /scripts/client/cpp/Connecting.cpp

Calling Remote Procedures
-------------------------

The kRPC server provides *procedures* that a client can run. These procedures are arranged in groups
called *services* to keep things organized. The functionality for the services are defined in the
header files in ``krpc/services/...``. For example, all of the functionality provided by the
SpaceCenter service is contained in the header file ``krpc/services/space_center.hpp``.

To interact with a service, you must include its header file and create an instance of the service,
passing a :class:`krpc::Client` object to its constructor.

The following example demonstrates how to invoke remote procedures using the C++ client. It
instantiates the SpaceCenter service and calls :func:`krpc::services::SpaceCenter::active_vessel` to
get an object representing the active vessel (of type
:class:`krpc::services::SpaceCenter::Vessel`). It sets the name of the vessel and then prints out
its altitude:

.. literalinclude:: /scripts/client/cpp/RemoteProcedures.cpp

Many procedures return an object identifying something in the game, such as a vessel or
a part. See :doc:`Object Lifetime </tutorials/object-lifetime>` for how such an object
behaves when a game is loaded or what it identifies is destroyed.

Server Side Functions
---------------------

A server side function is code that runs inside the game, on the server. :func:`krpc::run` evaluates
one once and returns its value. Every value it reads comes from the same physics tick. For example,
the following reads the vessel's altitude and speed together:

.. literalinclude:: /scripts/client/cpp/RunFunction.cpp

Use ``run`` for a function with effects, such as setting a property. An event or a function stream
evaluates its function on every update, repeating the effects.

.. note::

   :func:`krpc::add_stream`, :func:`krpc::run` and :func:`krpc::add_event` are defined in
   ``krpc/function_stream.hpp``.

.. _cpp-client-streams:

Streaming Data
--------------

A common use case for kRPC is to continuously extract data from the game. The naive approach to do
this would be to repeatedly call a remote procedure, such as in the following which repeatedly
prints the position of the active vessel:

.. literalinclude:: /scripts/client/cpp/Streaming1.cpp

This approach requires significant communication overhead as request/response messages are
repeatedly sent between the client and server. kRPC provides a more efficient mechanism to achieve
this, called *streams*.

A stream repeatedly executes a procedure on the server (with a fixed set of argument values) and
sends the result to the client. It only requires a single message to be sent to the server to
establish the stream, which will then continuously send data to the client until the stream is
closed.

The following example does the same thing as above using streams:

.. literalinclude:: /scripts/client/cpp/Streaming2.cpp

It calls ``position_stream`` once at the start of the program to create the stream, and then
repeatedly prints the position returned by the stream. The stream is automatically closed when the
client disconnects.

A stream can be created for any function call (except property setters) by adding ``_stream`` to the
end of the functions name. This returns a stream object of type :class:`template <typename T>
krpc::Stream`, where ``T`` is the return type of the original function. The most recent value of the
stream can be obtained by calling :func:`krpc::Stream::operator()()`. A stream can be stopped and
removed from the server by calling :func:`krpc::Stream::remove()` on the stream object. All of a
clients streams are automatically stopped when it disconnects.

Each stream updates on its own, so two streams read one after the other can give values from
different physics ticks. To read several values from one physics tick, make the calls while
holding the tick, as described in :ref:`holding-a-tick`.

.. note::

   A stream runs on the server until you remove it or the connection closes, even once every copy
   of the stream object is destroyed. Each call to a ``_stream`` function adds another stream.

Synchronizing with Stream Updates
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

A common use case for kRPC is to wait until the value returned by a method or attribute changes, and
then take some action. kRPC provides two mechanisms to do this efficiently: *condition variables*
and *callbacks*.

Condition Variables
~~~~~~~~~~~~~~~~~~~

Each stream has a condition variable associated with it, that is notified whenever the value of the
stream changes. The condition variables are instances of ``krpc::Condition``, used like
``std::condition_variable`` over the re-entrant mutex ``krpc::RecursiveMutex``. A wait releases
every level of the thread's lock. These can be used to block the current thread of execution until
the value of the stream changes.

The following example waits until the abort button is pressed in game, by waiting for the value of
:func:`krpc::services::SpaceCenter::Control::abort` to change to true:

.. literalinclude:: /scripts/client/cpp/ConditionVariables.cpp

This code creates a stream, acquires a lock on the streams condition variable (by calling
``acquire``) and then repeatedly checks the value of ``abort``. It leaves the loop when it changes
to true.

The body of the loop calls ``wait`` on the stream, which causes the program to block until the value
changes. This prevents the loop from 'spinning' and so it does not consume processing resources
whilst waiting.

Callbacks
~~~~~~~~~

Streams allow you to register callback functions that are called whenever the value of the stream
changes. Callback functions should take a single argument, which is the new value of the stream, and
should return nothing.

For example the following program registers two callbacks that are invoked when the value of
:func:`krpc::services::SpaceCenter::Control::abort` changes:

.. literalinclude:: /scripts/client/cpp/Callbacks.cpp

A callback added to a stream that already has a value is called with that value straight away.

.. note::

   The callback function may be called from a different thread than the one that created the
   stream. Any changes to shared state must therefore be protected with appropriate
   synchronization.

Stream Endings
^^^^^^^^^^^^^^

The following example streams the temperature of a part until the part is destroyed:

.. literalinclude:: /scripts/client/cpp/StreamEnd.cpp

Once a stream ends, reading the stream or waiting on it throws the exception that ended it. A stream
ends when its procedure throws an exception, when the server removes it, when you remove it, or when
the connection closes.

A stream that the server removes, such as an event that has finished, ends with
``krpc::services::KRPC::StreamRemovedException``. A stream you remove ends with
``krpc::StreamError``, and a closed connection with ``krpc::ConnectionError``.

An exception for an object that is not loaded, or a procedure not available in the game scene,
holds the stream instead. It resumes once the object or scene is available.

.. seealso::

   :doc:`/tutorials/object-lifetime`

To handle an ending in a callback, add an end callback with
:func:`krpc::Stream::add_end_callback`. It is called once, with the exception.

Streaming Server Side Functions
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Pass a server side function to :func:`krpc::add_stream` to stream its value. The server evaluates
the function on each stream update, within one physics tick. The value can be any type the server
can send, including collections and objects. For example, the following streams the vessel's
altitude in kilometers:

.. literalinclude:: /scripts/client/cpp/FunctionStream.cpp

The template type must describe the function's value, or :func:`krpc::add_stream` throws
``std::invalid_argument``. A nested position that can hold null, such as a list element, needs a
``std::optional``. The value itself can be a ``std::optional`` or not, and reading a null without
one throws ``std::runtime_error``.

A function stream or event ends on any exception its function throws. This includes
``krpc::services::KRPC::ObjectNotLoadedException``, which a game load or an unloaded vessel causes.
To keep running through a load, the function catches that exception with ``Expr::try_catch``.

.. _cpp-client-events:

Events
------

Some procedures return event objects of type :class:`krpc::Event`. These allow you to wait until an
event occurs, by calling :func:`krpc::Event::wait`. Under the hood, these are implemented using
streams and condition variables.

A call to ``wait`` returns straight away if the event has fired since the previous call on the same
event object, so no firing is missed between two calls. Several firings between two calls make
``wait`` return once.

You can also create a custom event from a server side function. The following builds the function
``mean_altitude > 1000``, and creates an event that triggers when the function returns true:

.. literalinclude:: /scripts/client/cpp/Event.cpp

Client API Reference
--------------------

.. namespace:: krpc

.. function:: Client connect(const std::string& name = "", const std::string& address = "127.0.0.1", unsigned int rpc_port = 50000, unsigned int stream_port = 50001, std::chrono::milliseconds timeout = std::chrono::milliseconds::zero())

   This function creates a connection to a kRPC server. It returns a :class:`krpc::Client` object,
   through which the server can be communicated with.

   :parameters:

      * **name** (*std::string*) -- A descriptive name for the connection. This is passed to the
        server and appears in the in-game server window.
      * **address** (*std::string*) -- The address of the server to connect to. Can either be a
        hostname or an IP address in dotted decimal notation. Defaults to '127.0.0.1'.
      * **rpc_port** (*unsigned int*) -- The port number of the RPC Server. Defaults to 50000. This
        should match the RPC port number of the server you want to connect to.
      * **stream_port** (*unsigned int*) -- The port number of the Stream Server. Defaults
        to 50001. This should match the stream port number of the server you want to connect to.
      * **timeout** (*std::chrono::milliseconds*) -- How long to wait for a connection before
        giving up. Defaults to zero, which waits indefinitely. A network that drops a connection
        attempt rather than refusing it otherwise leaves the client waiting.

.. function:: Client connect_local(const std::string& name = "", const std::string& rpc_path = "", const std::string& stream_path = "")

   This function creates a connection to a kRPC server running on the same machine, over unix
   domain sockets rather than TCP/IP. It returns a :class:`krpc::Client` object, just as
   :func:`krpc::connect` does, and the connection behaves identically thereafter.

   Unix domain sockets are available on Linux, macOS, and Windows 10 1803 and Windows Server
   2019 or later.

   :parameters:

      * **name** (*std::string*) -- A descriptive name for the connection. This is passed to the
        server and appears in the in-game server window.
      * **rpc_path** (*std::string*) -- The path of the socket the RPC Server is listening on. This
        should match the RPC socket path shown in the in-game server window. Leave empty for the
        path the server uses unless it was configured with another.
      * **stream_path** (*std::string*) -- The path of the socket the Stream Server is listening
        on. This should match the stream socket path shown in the in-game server window. Defaults
        as ``rpc_path`` does.

.. function:: template <typename T> Stream<T> add_stream(const services::KRPC::Expression& function)

   Create a stream from a server side function, evaluated on each update. Each call creates a new
   stream. Throws ``std::invalid_argument`` if the template type does not describe the function's
   value, and ``StreamError`` if the client has no stream connection. Throws
   ``krpc::services::KRPC::ArgumentException`` if the function has parameters.

   Reading a null value throws ``std::runtime_error`` unless the template type is a
   ``std::optional``. In a callback, that error is written to ``stderr`` and the callback does not
   run.

.. function:: template <typename T = void, typename... Args> T run(const services::KRPC::Expression& function, const Args&... args)

   Run a server side function within a single physics tick, and return its value, or run it for
   its effects when the template type is ``void``. The arguments are passed to the function's
   parameters, with ``std::nullopt`` or ``nullptr`` for a null.

   A number converts to a ``double`` parameter, and an integer or a ``float`` to a ``float``
   parameter, as C++ converts it implicitly, which can round. An integer converts to a signed
   parameter that holds every value of its type, and an integer that is not negative to an
   unsigned parameter at least as wide. A number a collection or a tuple holds converts the same
   way.

   Throws ``std::invalid_argument`` if the template type does not describe the function's value,
   the number of arguments differs from the number of parameters, or an argument does not match
   its parameter. Throws ``std::runtime_error`` for a null value unless the template type is a
   ``std::optional``.

   A class, enumeration or structure argument is checked by kind only. The server checks its
   class, and a null for a parameter that is not nullable, and throws
   ``krpc::services::KRPC::ArgumentException`` for a mismatch.

.. function:: Event add_event(const services::KRPC::Expression& function)

   Create an event from a server side function that evaluates to a boolean value. Each call
   creates a new event. Throws ``StreamError`` if the client has no stream connection, and
   ``krpc::services::KRPC::ArgumentException`` if the function does not evaluate to a boolean
   value or has parameters.

.. class:: Client

   This class provides the interface for communicating with the server. It is used by service class
   instances to invoke remote procedure calls. Instances of this class can be obtained by calling
   :func:`krpc::connect`.

   .. function:: ~Client()

      Destructs the client object and closes the connection to the server.

   .. function:: Condition& get_stream_update_condition() const

      A condition variable that is notified whenever a stream update message finishes processing.

   .. function:: std::unique_lock<RecursiveMutex>& get_stream_update_condition_lock() const

      A lock on the re-entrant mutex for the stream update condition variable. Every copy of the
      client shares the lock, so one thread at a time uses it.

   .. function:: void acquire_stream_update()

      Locks the mutex for the stream update condition variable.

   .. function:: void release_stream_update()

      Unlocks the mutex for the stream update condition variable.

   .. function:: void wait_for_stream_update(double timeout = -1)

      This method blocks until a stream update message finishes processing or the operation times out.

      The stream update condition variable must be locked (by calling ``acquire_stream_update``)
      before calling this method.

      If *timeout* is specified and is greater than or equal to 0, it is the timeout in seconds for
      the operation.

   .. function:: int add_stream_update_callback(const std::function<void()>& callback)

      Adds a callback function that is invoked whenever a stream update message finishes
      processing. Returns a unique identifier for the callback which can be used to remove it.

      .. note::

         The callback function may be called from a different thread than the one that created
         the stream. Any changes to shared state must therefore be protected with appropriate
         synchronization.

   .. function:: void remove_stream_update_callback(int tag)

      Removes a stream update callback function. The tag is the identifier returned when the
      callback was added.

.. class:: template <typename T> Stream

   This class represents a stream. See :ref:`cpp-client-streams`.

   Streams are created by calling a remote procedure with ``_stream`` appended to its name.

   Stream objects are copy constructible and assignable, and every copy refers to the same stream.
   Call :func:`remove` to remove the stream from the server.

   .. function:: float rate() const
   .. function:: void set_rate(float rate)

      The update rate of the stream in Hertz. When set to zero, the rate is unlimited.

   .. function:: T operator()()

      Get the most recently received value from the stream, waiting for the first value if none has
      arrived yet. Throws the exception that ended the stream, if it has ended, such as an exception
      thrown by the remote procedure.

   .. function:: Condition& get_condition() const

      A condition variable that is notified whenever the value of the stream changes. Every copy
      of the stream object shares it.

   .. function:: std::unique_lock<RecursiveMutex>& get_condition_lock() const

      A lock on the re-entrant mutex for the condition variable. Each stream object has its own
      lock, used by one thread, so a thread waiting on a stream uses its own copy of the stream
      object.

   .. function:: void acquire()

      Locks the mutex for the condition variable. Every copy of the stream object shares the
      mutex, so a lock taken through one copy can be released through another.

   .. function:: void release()

      Unlocks the mutex for the condition variable.

   .. function:: bool wait(double timeout = -1)

      This method blocks until the value of the stream changes or the operation times out. Returns
      true if the value changed, and false on a timeout.

      The method locks the condition variable itself, unless the thread holds it. To wait for a
      change from a value you have read, call ``acquire`` before reading it, so that no update
      lands between the read and the wait.

      If *timeout* is specified and is greater than or equal to 0, it is the timeout in seconds for
      the operation. Throws the exception that ended the stream, if it ends before or during the
      wait.

   .. function:: int add_callback(const std::function<void(T)>& callback)

      Adds a callback function that is invoked whenever the value of the stream changes. The
      callback function should take one argument, which is passed the new value of the
      stream. If the stream already has a value, the callback is invoked with it straight away.
      An error from the stream does not invoke the callback. Calling the stream throws it.
      Returns a unique identifier for the callback which can be used to remove it.

      .. note::

         The callback function may be called from a different thread than the one that created
         the stream. Any changes to shared state must therefore be protected with appropriate
         synchronization.

   .. function:: void remove_callback(int tag)

      Removes a callback from the stream. The tag is the identifier returned when the callback was
      added.

   .. function:: int add_end_callback(const std::function<void(std::exception_ptr)>& callback)

      Adds a callback function that is invoked once when the stream ends. The callback function
      is passed the exception that ended the stream. It is invoked straight away if the stream has
      already ended. Returns a unique identifier for the callback which can be used to remove it.

   .. function:: void remove_end_callback(int tag)

      Removes an end callback from the stream. The tag is the identifier returned when the
      callback was added.

   .. function:: void remove()

      Removes the stream from the server. Every copy of the stream object refers to the same
      stream. Another stream added for the same call stays live.

   .. function:: bool operator==(const Stream<T>& rhs)

      Returns true if the two stream objects are bound to the same stream. Each call to a
      ``_stream`` function adds a new stream, so two streams added for the same call are not
      equal.

   .. function:: bool operator!=(const Stream<T>& rhs)

      Returns true if the two stream objects are bound to different streams.

   .. function:: operator bool()

      Returns whether the stream object is bound to a stream.

.. class:: Event

   This class represents an event. See :ref:`cpp-client-events`. It is wrapper around a
   ``Stream<bool>`` that indicates when the event occurs.

   Event objects are copy constructible and assignable, and every copy refers to the same event.
   Call :func:`remove` to remove the event from the server.

   .. function:: Condition& get_condition() const

      A condition variable that is notified whenever the event occurs. Every copy of the event
      object shares it.

   .. function:: std::unique_lock<RecursiveMutex>& get_condition_lock() const

      A lock on the re-entrant mutex for the condition variable. Each event object has its own
      lock, used by one thread, so a thread waiting on an event uses its own copy of the event
      object.

   .. function:: void acquire()

      Locks the mutex for the condition variable. Every copy of the event object, and its
      underlying stream, shares the mutex.

   .. function:: void release()

      Unlocks the mutex for the condition variable.

   .. function:: bool wait(double timeout = -1)

      This method blocks until the event occurs or the operation times out. It returns straight
      away if the event has occurred since the previous call on this event object. Returns true if
      the event occurred, and false on a timeout.

      The method locks the condition variable itself, unless the thread holds it.

      If *timeout* is specified and is greater than or equal to 0, it is the timeout in seconds for
      the operation. Throws the exception that ended the event, if it ends before or during the
      wait.

   .. function:: int add_callback(const std::function<void()>& callback)

      Adds a callback function that is invoked whenever the event occurs. The callback function
      should be a function that takes zero arguments. If the event has occurred since the previous
      call to ``wait`` on this event object, and no callback was invoked for it, the callback is
      invoked once straight away. A callback added after the event has ended is not invoked for
      an earlier firing. An error from the event does not invoke the callback. Waiting on the
      event throws it. Returns a unique identifier for the callback which can be used to remove
      the callback.

   .. function:: void remove_callback(int tag)

      Removes a callback from the event. The tag is the identifier returned when the callback was
      added.

   .. function:: int add_end_callback(const std::function<void(std::exception_ptr)>& callback)

      Adds a callback function that is invoked once when the event ends, with the exception that
      ended it. It is invoked straight away if the event has already ended. Returns a unique
      identifier for the callback which can be used to remove it.

   .. function:: void remove_end_callback(int tag)

      Removes an end callback from the event. The tag is the identifier returned when the callback
      was added.

   .. function:: void remove()

      Removes the event from the server.

   .. function:: Stream<bool> stream()

      Returns the underlying stream for the event.

   .. function:: bool operator==(const Event& rhs)

      Returns true if the two event objects are bound to the same underlying stream.

   .. function:: bool operator!=(const Event& rhs)

      Returns true if the two event objects are bound to different underlying streams.

   .. function:: operator bool()

      Returns whether the event object is bound to a stream.

.. class:: RecursiveMutex

   The mutex of a stream's, an event's or the stream update's condition variable. The thread
   holding it can lock it again, and each lock is matched by an unlock. It provides ``lock``, ``try_lock``, ``try_lock_for``,
   ``try_lock_until`` and ``unlock``, so it can be held with ``std::unique_lock`` and
   ``std::lock_guard``.

   .. function:: bool held_by_this_thread() const

      Whether the calling thread holds the mutex.

.. class:: Condition

   The condition variable of a stream, an event or the stream update. Its ``wait``, ``wait_for``
   and ``wait_until`` take a ``std::unique_lock<RecursiveMutex>``, with or without a predicate,
   as ``std::condition_variable`` does. A wait releases every lock the calling thread holds on
   the mutex, and takes them again before it returns.

   .. function:: void wait()
   .. function:: template <typename Rep, typename Period> std::cv_status wait_for(const std::chrono::duration<Rep, Period>& duration)
   .. function:: template <typename Clock, typename Duration> std::cv_status wait_until(const std::chrono::time_point<Clock, Duration>& deadline)

      Waits as the overloads taking a lock do, for a thread that holds the mutex through
      ``acquire`` or a lock of its own.

   .. function:: void notify_one()
   .. function:: void notify_all()

.. function:: template <typename T> std::size_t hash_value(const T& value)

   Returns a hash of a value of any type the client carries: the arithmetic types, strings,
   enumerations, class objects, tuples, and the list, set and dictionary collection types,
   nested however deeply. A structure is also hashed by this function, and is given a
   ``std::hash`` of its own, so an unordered container can be keyed by one directly.

.. class:: hash

   A function object that calls :func:`hash_value`, to give a standard container that takes
   its hash as a template argument. The standard library hashes neither ``std::tuple`` nor
   any of the collection types, so a container keyed by one needs this:

   .. code-block:: cpp

      std::unordered_set<std::tuple<double, double, double>, krpc::hash> points;
