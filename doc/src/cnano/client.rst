.. default-domain:: c
.. highlight:: c

C-nano Client
=============

This client provides a C API for interacting with a kRPC server. It is intended for use on embedded
systems with tight resource constraints, hence the "nano" in its name.

Installing the Library
----------------------

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

Using the configure script
^^^^^^^^^^^^^^^^^^^^^^^^^^

You can build and install the client library and headers using the configure script provided with
the source. `Download the source archive <https://github.com/krpc/krpc/releases>_`, extract it
and then execute the following:

.. code-block:: bash

   ./configure
   make
   sudo make install
   sudo ldconfig

Using CMake
^^^^^^^^^^^

Alternatively, you can install the client library and headers using CMake.
`Download the source archive <https://github.com/krpc/krpc/releases>_`, extract it and execute the
following:

.. code-block:: bash

   cmake .
   make
   sudo make install
   sudo ldconfig

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
   * ``PB_NO_ERRMSG`` -- disables error messages in the nanopb library, which kRPC uses to
     communicate with the server. Enabled by default on in the Arduino version of the library.

 * Communication

   * ``KRPC_COMMUNICATION_POSIX`` -- Specifies that the library should be built to communicate over
     a serial port using POSIX read/write functions communication mechanisms. This is the default,
     unless the a different platform is detected.
   * ``KRPC_COMMUNICATION_ARDUINO`` -- Specifies that the library should be built using Arduino
     serial communication mechanisms. The Arduino platform will be auto-detected so you do not need
     to specify this manually.
   * ``KRPC_COMMUNICATION_CUSTOM`` -- Allows you to provide your own implementation for the
     communication mechanism.

 * Memory allocation

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

   On embedded systems you probably want to define ``KRPC_NO_PRINT_ERROR`` and ``PB_NO_ERRMSG`` to
   minimize the memory footprint of kRPC.

Configuring the Server
----------------------

The C-nano client library communicates with the server over a serial port using `protobuf messages
<https://github.com/nanopb/nanopb>`_. The kRPC server, which runs in the game, needs to be
configured to use the serial port protocol (instead of the default TCP/IP protocol). This can be
done from the in-game server configuration window, which also allows settings such as the port name
and baud rate to be configured.

Using the Library on a POSIX System
-----------------------------------

On POSIX systems (such as Linux) the following example program connects to the server, queries it
for its version and prints it out:

.. literalinclude:: /scripts/client/cnano/Basic.c

To compile this program using GCC, save the source as ``main.c`` and run the following:

.. code-block:: bash

   gcc main.c -lkrpc_cnano

The :func:`krpc_connect` function is used to open a connection to a server. It takes as its first
argument a connection object into which the connection information is written. This is passed to
subsequent calls to interact with the server. The second argument is a name for the connection
(displayed in game) and the third is the name of the serial port to connect over.

Using the Library on from an Arduino
------------------------------------

The following example demonstrates how to connect to the server from an Arduino, through its serial
port interface:

.. literalinclude:: /scripts/client/cnano/BasicArduino.ino
   :language: c

.. note::

   The main include file and include directory are named ``krpc`` instead of ``krpc_cnano`` in the
   Arduino version of the library.

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

.. _cnano-client-streams:
.. _cnano-client-events:

Streams and Events
------------------

These features are not yet supported by this client.

Client API Reference
--------------------

.. function:: krpc_error_t krpc_open(krpc_connection_t * connection, const krpc_connection_config_t * arg)

   Create a communication handle over which the client can talk to a server.

   When the library is built using ``KRPC_COMMUNICATION_POSIX`` (which is defined by default)
   calling this function opens a serial port using the port name passed as *arg*, using a call to
   ``open(arg, ...)``. In this case the type of the *arg* parameter is ``const char *``. For example:

   .. code-block:: c

     krpc_connection_t conn;
     krpc_open(&conn, "COM0");

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
