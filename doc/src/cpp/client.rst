.. default-domain:: cpp
.. highlight:: cpp

C++ Client
==========

This client provides a C++ API for interacting with a kRPC server.

Installing the Library
----------------------

Dependencies
^^^^^^^^^^^^

First you need to install kRPC's dependencies: `ASIO <http://think-async.com/>`_ which is used for
network communication and `protobuf <https://github.com/google/protobuf>`_ which is used to
serialize messages.

ASIO is a headers-only library. The boost version is not required, installing the non-Boost variant
is sufficient. On Ubuntu, this can be done using apt:

.. code-block:: bash

   sudo apt-get install libasio-dev

Alternatively it can be downloaded `from the ASIO website <http://think-async.com/Asio/Download>`_.

Protobuf version 3 is also required, and can be `downloaded from GitHub
<https://github.com/google/protobuf/releases>`_. Installation instructions `can be found here
<https://github.com/google/protobuf/blob/master/src/README.md>`_.

Using the configure script
^^^^^^^^^^^^^^^^^^^^^^^^^^

Once the dependencies have been installed, you can install the kRPC client library and headers using
the configure script provided with the source. :github-download-zip:`Download the source archive
<krpc-cpp>`, extract it and then execute the following:

.. code-block:: bash

   ./configure
   make
   sudo make install
   sudo ldconfig

Using CMake
^^^^^^^^^^^

Alternatively, you can install the client library and headers using
CMake. :github-download-zip:`Download the source archive <krpc-cpp>`, extract it and execute the
following:

.. code-block:: bash

   cmake .
   make
   sudo make install
   sudo ldconfig

Manual installation
^^^^^^^^^^^^^^^^^^^

The library is fairly simple to build manually if you can't use the configure script or CMake. The
headers are in the ``include`` directory and the source files are in ``src``.

Using the Library
-----------------

A kRPC program needs to be compiled with C++11 support enabled, and linked against ``libkrpc`` and
``libprotobuf``. The following example program connects to the server, queries it for its version
and prints it out:

.. literalinclude:: /scripts/client/cpp/Basic.cpp

To compile this program using GCC, save the source as ``main.cpp`` and run the following:

.. code-block:: bash

   g++ main.cpp -std=c++11 -lkrpc -lprotobuf

.. note::

   If you get linker errors claiming that there are undefined references to
   ``google::protobuf::...`` you probably have an older version of protobuf installed on your
   system. In this case, replace ``-lprotobuf`` with ``-l:libprotobuf.so.10`` in the above command
   so that GCC uses the correct version of the library.

Connecting to the Server
------------------------

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

.. _cpp-client-streams:

Streaming Data from the Server
------------------------------

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

Updates to streams can be paused by calling :func:`krpc::Client::freeze_streams()`. After this call,
all streams will have their values frozen to values from the same physics tick. Updates can be
resumed by calling :func:`krpc::Client::thaw_streams()`. This is useful if you need to perform some
computation using stream values and require all of the stream values to be from the same physics
tick.

Synchronizing with Stream Updates
---------------------------------

A common use case for kRPC is to wait until the value returned by a method or attribute changes, and
then take some action. kRPC provides two mechanisms to do this efficiently: *condition variables*
and *callbacks*.

Condition Variables
^^^^^^^^^^^^^^^^^^^

Each stream has a condition variable associated with it, that is notified whenever the value of the
stream changes. The condition variables are instances of ``std::condition_variable``. These can be
used to block the current thread of execution until the value of the stream changes.

The following example waits until the abort button is pressed in game, by waiting for the value of
:func:`krpc::services::SpaceCenter::Control::abort` to change to true:

.. literalinclude:: /scripts/client/cpp/ConditionVariables.cpp

This code creates a stream, acquires a lock on the streams condition variable (by calling
``acquire``) and then repeatedly checks the value of ``abort``. It leaves the loop when it changes
to true.

The body of the loop calls ``wait`` on the stream, which causes the program to block until the value
changes. This prevents the loop from 'spinning' and so it does not consume processing resources
whilst waiting.

.. note::

   The stream does not start receiving updates until the first call to ``wait``. This means that the
   example code will not miss any updates to the streams value, as it will have already locked the
   condition variable before the first stream update is received.

Callbacks
^^^^^^^^^

Streams allow you to register callback functions that are called whenever the value of the stream
changes. Callback functions should take a single argument, which is the new value of the stream, and
should return nothing.

For example the following program registers two callbacks that are invoked when the value of
:func:`krpc::services::SpaceCenter::Control::abort` changes:

.. literalinclude:: /scripts/client/cpp/Callbacks.cpp

.. note::

   When a stream is created it does not start receiving updates until ``start`` is called. This is
   implicitly called when accessing the value of a stream, but as this example does not do this an
   explicit call to ``start`` is required.

.. note::

   The callbacks are registered before the call to ``start`` so that stream updates are not missed.

.. note::

   The callback function may be called from a different thread to that which created the stream. Any
   changes to shared state must therefore be protected with appropriate synchronization.

.. _cpp-client-events:

Custom Events
-------------

Some procedures return event objects of type :class:`krpc::Event`. These allow you to wait until an
event occurs, by calling :func:`krpc::Event::wait`. Under the hood, these are implemented using
streams and condition variables.

Custom events can also be created. An expression API allows you to create code that runs on the
server and these can be used to build a custom event. For example, the following creates the
expression ``mean_altitude > 1000`` and then creates an event that will be triggered when the
expression returns true:

.. literalinclude:: /scripts/client/cpp/Event.cpp

Client API Reference
--------------------

.. namespace:: krpc

.. function:: Client connect(const std::string& name = "", const std::string& address = "127.0.0.1", unsigned int rpc_port = 50000, unsigned int stream_port = 50001)

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

.. class:: Client

   This class provides the interface for communicating with the server. It is used by service class
   instances to invoke remote procedure calls. Instances of this class can be obtained by calling
   :func:`krpc::connect`.

   .. function:: ~Client()

      Destructs the client object and closes the connection to the server.

   .. function:: void freeze_streams()

      Pause stream updates, after the next stream update message is received. This function blocks
      until the streams have been frozen.

   .. function:: void thaw_streams()

      Resume stream updates. Before this function returns, the last received update message is
      applied to the streams.

.. class:: template <typename T> Stream

   This class represents a stream. See :ref:`cpp-client-streams`.

   Streams are created by calling a remove procedure with ``_stream`` appended to its name.

   Stream objects are copy constructible and assignable. A stream is removed from the server when
   all stream objects that refer to it are destroyed.

   .. function:: void start(bool wait = true)

      Starts the stream. When a stream is created it does not start sending updates to the client
      until this method is called.

      If wait is true, this method will block until at least one update has been received from the
      server.

      If wait is false, the method starts the stream and returns immediately. Subsequent calls to
      ``operator()`` may throw a ``krpc::StreamError`` exception.

   .. function:: T operator()()

      Get the most recently received value from the stream.

   .. function:: std::condition_variable& get_condition() const

      A condition variable that is notified whenever the value of the stream changes.

   .. function:: std::unique_lock<std::mutex>& get_condition_lock() const

      The lock for the condition variable.

   .. function:: void acquire()

      Acquires a lock on the mutex for the condition variable.

   .. function:: void release()

      Releases the lock on the mutex for the condition variable.

   .. function:: void wait(double timeout = -1)

      This method blocks until the value of the stream changes or the operation times out.

      The streams condition variable must be locked (by calling ``acquire``) before calling this
      method.

      If *timeout* is specified and is greater than or equal to 0, it is the timeout in seconds for
      the operation.

      If the stream has not been started this method calls ``start(false)`` to start the stream
      (without waiting for at least one update to be received).

   .. function:: void add_callback(const std::function<void(T)>& callback)

      Adds a callback function that is invoked whenever the value of the stream changes. The
      callback function should take one argument, which is passed the new value of the stream.

      .. note::

         The callback function may be called from a different thread to that which created the
         stream. Any changes to shared state must therefore be protected with appropriate
         synchronization.

   .. function:: void remove()

      Removes the stream from the server.

   .. function:: bool operator==(const Stream<T>& rhs)

      Returns true if the two stream objects are bound to the same stream.

   .. function:: bool operator!=(const Stream<T>& rhs)

      Returns true if the two stream objects are bound to different streams.

   .. function:: operator bool()

      Returns whether the stream object is bound to a stream.

.. class:: Event

   This class represents an event. See :ref:`cpp-client-events`. It is wrapper around a
   ``Stream<bool>`` that indicates when the event occurs.

   Event objects are copy constructible and assignable. An event is removed from the server when all
   event objects that refer to it are destroyed.

   .. function:: void start()

      Starts the event. When an event is created, it will not receive updates from the server until
      this method is called.

   .. function:: std::condition_variable& get_condition() const

      The condition variable that is notified whenever the event occurs.

   .. function:: std::unique_lock<std::mutex>& get_condition_lock() const

      The lock for the condition variable.

   .. function:: void acquire()

      Acquires a lock on the mutex for the condition variable.

   .. function:: void release()

      Releases the lock on the mutex for the condition variable.

   .. function:: void wait(double timeout = -1)

      This method blocks until the event occurs or the operation times out.

      The events condition variable must be locked before calling this method.

      If *timeout* is specified and is greater than or equal to 0, it is the timeout in seconds for
      the operation.

      If the event has not been started this method calls ``start()`` to start the underlying
      stream.

   .. function:: void add_callback(const std::function<void()>& callback)

      Adds a callback function that is invoked whenever the event occurs. The callback function
      should be a function that takes zero arguments.

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
