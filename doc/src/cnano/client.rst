.. default-domain:: c
.. highlight:: c

C-nano Client
=============

This client provides a C API for interacting with a kRPC server. It is intended for use on embedded
systems with tight resource constraints, hence the "nano" in its name.

Installing
----------

Manually include the source in your project
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The source files can be included in your project manually, by downloading and extracting the
source archive from the `GitHub release page <https://github.com/krpc/krpc/releases>`_.
The header files can be found in the ``include`` directory and the source files are in ``src``.

Arduino Library Manager
^^^^^^^^^^^^^^^^^^^^^^^

If you are writing an Arduino sketch, the library can be installed using the Arduino Library Manager
by searching for and installing "kRPC".

.. note::

   The source files installed by the Arduino Library Manager are renamed to end with ``.cpp`` so
   that they are built using the C++ compiler. This allows the library to use the C++ only
   ``HardwareSerial`` class for communication.

vcpkg
^^^^^

The C-nano client is available from `vcpkg <https://vcpkg.io>`_. It can be installed as follows:

.. tabs::

   .. tab:: Linux

      .. code-block:: bash

         vcpkg install krpc-cnano

   .. tab:: Windows

      .. code-block:: bash

         vcpkg install krpc-cnano:x64-windows

The library communicates over a serial port unless the ``tcp`` feature is asked for, which builds
it to communicate over TCP/IP instead:

.. tabs::

   .. tab:: Linux

      .. code-block:: bash

         vcpkg install "krpc-cnano[tcp]"

   .. tab:: Windows

      .. code-block:: bash

         vcpkg install "krpc-cnano[tcp]:x64-windows"

CMake
^^^^^

CMake 3.15 or later is required.  `Download the source archive
<https://github.com/krpc/krpc/releases>`_, extract it and run:

.. code-block:: bash

   cmake -B build
   cmake --build build
   cmake --install build

By default, CMake looks for a system-installed nanopb. To download nanopb automatically
at build time instead, pass ``-DKRPC_FETCH_DEPS=ON`` (or ``-DKRPC_FETCH_NANOPB=ON``) to
the configure step. When ``OFF`` (the default) the system package is required.

To install to a custom prefix:

.. code-block:: bash

   cmake -B build -DCMAKE_INSTALL_PREFIX=/install/path
   cmake --build build
   cmake --install build

Compilation Options
^^^^^^^^^^^^^^^^^^^

The following options control how the library operates. They must be specified at compile time as an
argument to the compiler.

* Error handling

  * ``KRPC_ERROR_CHECK_RETURN`` (the default) -- when a remote procedure call gets an error, it
    returns the error code.
  * ``KRPC_ERROR_CHECK_EXIT`` -- terminates the program (by calling ``exit()``) when an error
    occurs in a remote procedure call.
  * ``KRPC_ERROR_CHECK_ASSERT`` -- fails a debug assertion (by calling ``assert()``) when an error
    occurs in a remote procedure call.
  * ``KRPC_ERROR_CHECK_FN`` -- specifies the ``krpc_error_handler`` function should be called when
    an error occurs in a remote procedure call. This should be set to a pointer to a function that
    takes a single parameter of type ``krpc_error_t``.

  * ``KRPC_PRINT_ERRORS_TO_STDERR`` -- enables printing of a descriptive error message to stderr
    when an error occurs
  * ``KRPC_ERROR_MESSAGES`` -- captures the message describing an error returned by the server,
    which can then be read using :func:`krpc_get_error_message`. Without this every server error
    is indistinguishable, as they all return :macro:`KRPC_ERROR_RPC_FAILED`. Enabling it costs
    ``KRPC_ERROR_MESSAGE_LENGTH`` bytes of static storage.
  * ``KRPC_ERROR_MESSAGE_LENGTH`` -- the size of the buffer used to store the message described
    above, defaulting to 256 bytes. Longer messages are truncated to fit.
  * ``PB_NO_ERRMSG`` -- disables error messages in the nanopb library, which kRPC uses to
    communicate with the server. Enabled by default in the Arduino version of the library.

* Communication

  * ``KRPC_COMMUNICATION_POSIX`` -- Specifies that the library should be built to communicate over
    a serial port using POSIX read/write functions communication mechanisms. This is the default
    when no other platform is detected.
  * ``KRPC_COMMUNICATION_WINDOWS`` -- Specifies that the library should be built to communicate
    over a serial port using the Windows API. The Windows platform will be auto-detected so you do
    not need to specify this manually.
  * ``KRPC_COMMUNICATION_ARDUINO`` -- Specifies that the library should be built using Arduino
    serial communication mechanisms. The Arduino platform will be auto-detected so you do not need
    to specify this manually.
  * ``KRPC_COMMUNICATION_TCP`` -- Specifies that the library should be built to communicate over
    TCP/IP with a server reachable over the network. A serial port remains the usual choice for the
    devices this client is written for, so this is never auto-detected and has to be specified.
    CMake builds of the library should ask for it with ``-DKRPC_COMMUNICATION_TCP=ON``, and vcpkg
    installs with the ``tcp`` feature, rather than by defining it directly, so that programs
    linking the library are built for the same transport and, on Windows, are linked against
    winsock along with it.
  * ``KRPC_COMMUNICATION_CUSTOM`` -- Allows you to provide your own implementation for the
    communication mechanism.
  * ``KRPC_SINGLE_CONNECTION`` -- Only meaningful alongside ``KRPC_COMMUNICATION_CUSTOM``. A serial
    port carries the RPC and stream connections over the one link, so every message sent over it is
    wrapped in a multiplexed message saying which connection it belongs to. A custom communication
    mechanism is assumed to work the same way; define this if yours instead opens a connection of
    its own to each server, as a socket does, and the messages are sent unwrapped.

* Memory allocation

  * ``KRPC_BUFFER_SIZE`` -- How much of a message to hold in memory while it is written to or read
    from the connection, defaulting to 1024 bytes, and to 128 bytes when building for Arduino where
    a kilobyte is a large share of the memory there is. A message larger than this is carried in as
    many passes as it takes, so this bounds the memory a call costs and never the size of a message
    it can carry. A message that fits is also cheaper to send, as its size can be written in front
    of it rather than found by a pass over it first, so it is worth setting this above the largest
    call a program makes. Two buffers of this size are used, one for a message being sent and one
    for a message being received, and neither outlives the call it is made for.
  * ``KRPC_ALLOC_BLOCK_SIZE`` -- The size of collections (lists, sets, etc.) are not know ahead of
    time, so when they are received from the server they are decoded into dynamically allocated
    memory on the heap. This option controls how many items to increase the capacity of the
    collection by when its space is exhausted. Setting this to 1 will consume the least amount of
    heap memory, but will require one heap allocation call per item. Setting this to a higher value
    will consume more memory, but require fewer allocations.
  * ``KRPC_CUSTOM_MEMORY_ALLOC`` -- Disables the default implementation of memory allocation
    functions ``krpc_malloc``, ``krpc_calloc``, ``krpc_recalloc`` and krpc_free so that you can
    provide your own implementation.

.. note::

   On embedded systems you probably want to leave ``KRPC_PRINT_ERRORS_TO_STDERR`` undefined and
   define ``PB_NO_ERRMSG`` to minimize the memory footprint of kRPC.

Getting Started
---------------

Configuring the Server
^^^^^^^^^^^^^^^^^^^^^^

The C-nano client library communicates with the server using `protobuf messages
<https://github.com/nanopb/nanopb>`_, over a serial port by default. The kRPC server, which runs in
the game, needs to be configured to use the serial port protocol (instead of the default TCP/IP
protocol). This can be done from the in-game server configuration window, which also allows settings
such as the port name, baud rate and parity settings.

Building the library with ``KRPC_COMMUNICATION_TCP`` instead has it communicate over TCP/IP, with a
server left on its default protocol. A connection is then opened with the address and port the
server is listening on rather than the name of a port.

.. note:: A serial port carries data far more slowly than the game produces it, and the server
          drops a connection that produces more data than the port can carry for a sustained
          period. See :ref:`communication-protocol-serialio-buffering` for the limits and how to
          stay within them. This does not apply over TCP/IP. The client blocks while waiting for
          data from the server, with no timeout, so a call that is never answered, for example
          because the connection was dropped, never returns.

Linking
^^^^^^^

After installation, CMake projects can link the library with:

.. code-block:: cmake

   find_package(krpc_cnano CONFIG REQUIRED)
   target_link_libraries(my_app PRIVATE krpc_cnano::krpc_cnano)

When installed via vcpkg, pass the vcpkg toolchain file at configure time:

.. code-block:: bash

   cmake -B build -DCMAKE_TOOLCHAIN_FILE=/path/to/vcpkg/scripts/buildsystems/vcpkg.cmake


POSIX Systems
^^^^^^^^^^^^^

On POSIX systems (such as Linux) the following example program connects to the server, queries it
for its version and prints it out:

.. literalinclude:: /scripts/client/cnano/Basic.c

The :func:`krpc_connect` function is used to open a connection to a server. It takes as its first
argument a connection object into which the connection information is written. This is passed to
subsequent calls to interact with the server. The second argument is a name for the connection
(displayed in game) and the third is the name of the serial port to connect over.

Arduino
^^^^^^^

The following example demonstrates how to connect to the server from an Arduino, through its serial
port interface:

.. literalinclude:: /scripts/client/cnano/BasicArduino.ino
   :language: c

Calling Remote Procedures
-------------------------

The kRPC server provides *procedures* that a client can run. These procedures are arranged in groups
called *services* to keep things organized. The functionality for the services are defined in the
header files in ``krpc/services/...``. For example, all of the functionality provided by the
SpaceCenter service is contained in the header file ``krpc/services/space_center.h``.

The following example demonstrates how to invoke remote procedures using the Cnano client. It calls
:func:`krpc_SpaceCenter_ActiveVessel` to get a handle to the active vessel (of type
:type:`krpc_SpaceCenter_Vessel_t`). It sets the name of the vessel and then prints out its altitude:

.. literalinclude:: /scripts/client/cnano/RemoteProcedures.c

Many procedures return an object standing for something in the game, such as a vessel or
a part. See :doc:`Object Lifetime </tutorials/object-lifetime>` for what those objects do
when a game is loaded or the thing they stand for is destroyed.

.. _cnano-client-streams:
.. _cnano-client-events:

Streams and Events
------------------

These features are not supported by this client.

Client API Reference
--------------------

.. function:: krpc_error_t krpc_open(krpc_connection_t * connection, const krpc_connection_config_t * arg)

   Create a communication handle over which the client can talk to a server.

   When the library is built using ``KRPC_COMMUNICATION_POSIX`` (the default when no other
   platform is detected) calling this function opens a serial port using the port name passed as
   *arg*, using a call to ``open(arg, ...)``. In this case the type of the *arg* parameter is
   ``const char *``. For example:

   .. code-block:: c

     krpc_connection_t conn;
     krpc_open(&conn, "/dev/ttyS0");

   When the library is built using ``KRPC_COMMUNICATION_WINDOWS`` (auto-detected on Windows)
   calling this function opens the serial port named by *arg* using the Windows API, for example
   ``krpc_open(&conn, "COM1")``.

   When the library is built using ``KRPC_COMMUNICATION_TCP`` calling this function connects to the
   server over TCP/IP. *arg* is a pointer to a structure of type ``krpc_connection_config_t``
   holding the address of the machine the server is running on and the port its RPC server is
   listening on. The address may be a host name or an address literal, and every endpoint it
   resolves to is tried until one accepts the connection. For example:

   .. code-block:: c

     krpc_connection_t conn;
     krpc_connection_config_t config;
     config.address = "127.0.0.1";
     config.port = 50000;
     krpc_open(&conn, &config);

   When the library is built using ``KRPC_COMMUNICATION_ARDUINO``, *connection* must be a pointer to
   a ``HardwareSerial`` object. *arg* is optionally used to pass additional configuration options
   used to initialize the connection, including baud rate for the serial port.

   If *arg* is set to ``NULL`` the connection is initialized with a baud rate of 9600 and
   defaults ``SERIAL_8N1`` for data, parity and stop bits. For example:

   .. code-block:: c

     krpc_connection_t conn;
     krpc_open(&conn, NULL);

   When *arg* set to a pointer to a structure of type ``krpc_connection_config_t``, the baud rate, and data, parity and stop
   bits in the structure are used to initialize the connection. For example:

   .. code-block:: c

     krpc_connection_t conn;
     krpc_connection_config_t config;
     config.speed = 115200;
     config.config = SERIAL_5N1;
     krpc_open(&conn, &config);

   .. note::

     A serial connection has no end-of-file, so on Arduino a read fails with
     :macro:`KRPC_ERROR_EOF` when the serial timeout elapses without a single byte arriving.
     This timeout is one second by default and applies to each byte, so a slow but progressing
     reply never trips it. If the server can take longer than this to start replying, raise the
     timeout with ``Serial.setTimeout`` before calling :func:`krpc_open`.

.. function:: krpc_error_t krpc_connect(krpc_connection_t connection, const char * name)

   Connect to a kRPC server.

   :parameters:

      * **connection** (*krpc_connection_t*) -- A connection handle, created using a call to
        :func:`krpc_open`.
      * **name** (*const char\**) -- A descriptive name for the connection. This is passed to the
        server and appears in the in-game server window.

.. function:: krpc_error_t krpc_close(krpc_connection_t connection)

   Closes the communication handle.

.. type:: krpc_error_t

   All kRPC functions return error codes of this type.

   .. macro:: KRPC_OK

              The function completed successfully and no error occurred.

   .. macro:: KRPC_ERROR_IO

              An input/output error occurred when communicating with the server.

   .. macro:: KRPC_ERROR_EOF

              End of file was received from the server.

   .. macro:: KRPC_ERROR_CONNECTION_FAILED

              Failed to establish a connection to the server.

   .. macro:: KRPC_ERROR_NO_RESULTS

              The remote procedure call did not return a result.

   .. macro:: KRPC_ERROR_RPC_FAILED

              The remote procedure call threw an exception.

   .. macro:: KRPC_ERROR_ENCODING_FAILED

              The encoder failed to construct the remote procedure call.

   .. macro:: KRPC_ERROR_DECODING_FAILED

              The decoder failed to interpret a result sent by the server.

.. function:: const char * krpc_get_error(krpc_error_t error)

   Returns a descriptive string for the given error code.

.. function:: const char * krpc_get_error_message(void)

   Returns the message describing the most recent error returned by the server, or an empty
   string if there has not been one. It is formatted as ``service.name: description``, for
   example ``SpaceCenter.InvalidOperationException: Vessel does not exist``, followed by the
   server stack trace when the server is configured to send one, and is truncated to fit
   ``KRPC_ERROR_MESSAGE_LENGTH``.

   This function is only available when the library is built with ``KRPC_ERROR_MESSAGES``
   defined, as storing the message is not free.
