.. default-domain:: c
.. highlight:: c

C-nano Client
=============

This guide describes how to install the C-nano client, and the features it provides. The
:ref:`cnano-client-reference` describes each function and type.

The client is written for microcontrollers and other devices with little memory, hence the "nano"
in its name. For the full feature set from C, use the :doc:`C++ client </cpp/client>`.

Installing the Library
----------------------

Source Archive
^^^^^^^^^^^^^^

Download the source archive from the `GitHub release page <https://github.com/krpc/krpc/releases>`_,
and add its files to your project. The header files are in the ``include`` directory, and the
source files are in ``src``.

Arduino Library Manager
^^^^^^^^^^^^^^^^^^^^^^^

For an Arduino sketch, install the library from the Arduino Library Manager by searching for
"kRPC".

.. note::

   The Arduino Library Manager installs the source files with names ending in ``.cpp``, so that
   they are built with the C++ compiler. The library uses the C++ ``HardwareSerial`` class to
   communicate.

vcpkg
^^^^^

The client is available from `vcpkg <https://vcpkg.io>`_. To install it:

.. tabs::

   .. tab:: Linux

      .. code-block:: bash

         vcpkg install krpc-cnano

   .. tab:: Windows

      .. code-block:: bash

         vcpkg install krpc-cnano:x64-windows

This builds the library to communicate over a serial port. The ``tcp`` feature builds it to
communicate over TCP/IP instead:

.. tabs::

   .. tab:: Linux

      .. code-block:: bash

         vcpkg install "krpc-cnano[tcp]"

   .. tab:: Windows

      .. code-block:: bash

         vcpkg install "krpc-cnano[tcp]:x64-windows"

The ``localsocket`` feature builds it to communicate over a local socket:

.. code-block:: bash

   vcpkg install "krpc-cnano[localsocket]"

A project in manifest mode lists the client in its ``vcpkg.json`` instead, with any feature it
needs, and vcpkg installs it when you configure the project. The following asks for the TCP/IP
build:

.. code-block:: json

   {
     "dependencies": [
       { "name": "krpc-cnano", "features": ["tcp"] }
     ]
   }

CMake
^^^^^

The build requires CMake 3.15 or later, a C99 compiler, ``protoc`` and Python 3. nanopb uses
``protoc`` and Python to generate its code. `Download the source archive
<https://github.com/krpc/krpc/releases>`_, extract it and run:

.. code-block:: bash

   cmake -B build
   cmake --build build
   cmake --install build

By default, CMake looks for a system-installed nanopb. Pass ``-DKRPC_FETCH_DEPS=ON`` or
``-DKRPC_FETCH_NANOPB=ON`` to the configure step to download nanopb at build time instead.

Pass ``-DKRPC_COMMUNICATION_TCP=ON`` or ``-DKRPC_COMMUNICATION_LOCALSOCKET=ON`` to build the
library to communicate over TCP/IP or a local socket. A program that links the library is then
built for the same protocol.

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

   find_package(krpc_cnano CONFIG REQUIRED)
   target_link_libraries(my_app PRIVATE krpc_cnano::krpc_cnano)

The library's target carries the protocol it was built for, so your program is built for the
same one.

For a client installed with vcpkg, pass the vcpkg toolchain file when you configure your project:

.. code-block:: bash

   cmake -B build -DCMAKE_TOOLCHAIN_FILE=/path/to/vcpkg/scripts/buildsystems/vcpkg.cmake

For a client installed in a directory of your choice, pass that directory as
``CMAKE_PREFIX_PATH``, so that ``find_package`` finds the client there:

.. code-block:: bash

   cmake -B build -DCMAKE_PREFIX_PATH=/path/to/install

Compiler Command Line
^^^^^^^^^^^^^^^^^^^^^

Link your program against ``libkrpc_cnano`` and nanopb's ``libprotobuf-nanopb``. For a program
saved as ``main.c``:

.. tabs::

   .. tab:: GCC

      .. code-block:: bash

         gcc main.c -lkrpc_cnano -lprotobuf-nanopb

   .. tab:: Clang

      .. code-block:: bash

         clang main.c -lkrpc_cnano -lprotobuf-nanopb

For a library built for TCP/IP or a local socket, also pass ``-DKRPC_COMMUNICATION_TCP`` or
``-DKRPC_COMMUNICATION_LOCALSOCKET``, so that your program is built for the same protocol.

.. _cnano-client-compilation-options:

Compilation Options
^^^^^^^^^^^^^^^^^^^

The following macros control how the library works. Define them when compiling, such as with the
compiler's ``-D`` argument.

* Error handling

  * ``KRPC_ERROR_CHECK_RETURN``, the default, returns the error code from the function that gets
    the error.
  * ``KRPC_ERROR_CHECK_EXIT`` calls ``exit()`` when an error occurs.
  * ``KRPC_ERROR_CHECK_ASSERT`` fails an ``assert()`` when an error occurs.
  * ``KRPC_ERROR_CHECK_FN`` calls :var:`krpc_error_handler` when an error occurs. Set it to a
    pointer to a function that takes a single parameter of type :type:`krpc_error_t`.
  * ``KRPC_PRINT_ERRORS_TO_STDERR`` prints a message describing each error to stderr.
  * ``KRPC_ERROR_MESSAGES`` stores the message of each error from the server, for
    :func:`krpc_get_error_message`. It costs ``KRPC_ERROR_MESSAGE_LENGTH`` bytes of static
    storage.
  * ``KRPC_ERROR_MESSAGE_LENGTH`` is the size of the buffer for that message, 256 bytes by
    default.
  * ``PB_NO_ERRMSG`` disables the error messages of the nanopb library, which kRPC uses to encode
    and decode messages. The Arduino version of the library defines it by default.

* Communication

  * ``KRPC_COMMUNICATION_POSIX`` communicates over a serial port with the POSIX ``read`` and
    ``write`` functions. This is the default when no other platform is detected.
  * ``KRPC_COMMUNICATION_WINDOWS`` communicates over a serial port with the Windows API. It is
    detected on Windows.
  * ``KRPC_COMMUNICATION_ARDUINO`` communicates over an Arduino ``HardwareSerial``. It is
    detected on Arduino.
  * ``KRPC_COMMUNICATION_TCP`` communicates over TCP/IP. On Windows, a program that links the
    library also links winsock.
  * ``KRPC_COMMUNICATION_LOCALSOCKET`` communicates over a local socket, with a server on the same
    machine.
  * ``KRPC_COMMUNICATION_CUSTOM`` lets you provide your own implementation of
    :func:`krpc_open`, :func:`krpc_close`, :func:`krpc_read` and :func:`krpc_write`.

* Memory allocation

  * ``KRPC_BUFFER_SIZE`` is how much of a message to hold in memory while it is written to or
    read from the connection. The default is 1024 bytes, and 128 bytes on Arduino. A larger
    message is carried in several passes. A message that fits is cheaper to send. Two buffers of
    this size are used, one for sending and one for receiving, and neither outlives the call.
  * ``KRPC_ALLOC_BLOCK_SIZE`` is how many items the library adds to the capacity of a collection
    it decodes into heap memory, each time the collection runs out of space. The default is 4. A
    value of 1 uses the least memory, and makes one allocation per item.
  * ``KRPC_CUSTOM_MEMORY_ALLOC`` disables the default implementations of :func:`krpc_malloc`,
    :func:`krpc_calloc`, :func:`krpc_recalloc` and :func:`krpc_free`, so that you can provide
    your own.

.. note::

   On a microcontroller, leave ``KRPC_PRINT_ERRORS_TO_STDERR`` undefined and define
   ``PB_NO_ERRMSG`` to minimize the memory kRPC uses.

Connecting to the Server
------------------------

The game must be running with the server started. :ref:`getting-started-server` in the
getting started guide shows how to install and start it.

The client connects over a serial port, TCP/IP or a local socket. The library is built for one of
them, chosen by the :ref:`compilation options <cnano-client-compilation-options>`. Each server in
the server window uses one protocol, and accepts connections over that protocol only. Pick the
protocol when you add the server, and build the library for the matching one.

Over a Serial Port
^^^^^^^^^^^^^^^^^^

Select "Protobuf over SerialIO" as the server's protocol. The server window also sets the port
name and the baud rate. The following connects to a server, and prints its version:

.. literalinclude:: /scripts/cnano/client/ConnectingSerial.c

:func:`krpc_open` opens the serial port named by its second argument, such as ``COM3`` on Windows
or ``/dev/ttyS0`` on Linux. It writes the connection into its first argument. You pass the
connection to every call. :func:`krpc_connect` then starts a session, with a name for the
connection that appears in the server window in the game.

On an Arduino, :func:`krpc_open` takes a pointer to a ``HardwareSerial``:

.. literalinclude:: /scripts/cnano/client/ConnectingArduino.ino
   :language: c

A client that resets can find part of a message from its earlier session on the line. The
example retries :func:`krpc_connect` until the server drops the partial message.

.. note::

   A serial port carries data far more slowly than the game produces it. The server drops a
   connection that sends more data than the port can carry.
   :ref:`communication-protocol-serialio-buffering` describes the limits.

Over TCP/IP
^^^^^^^^^^^

Select "Protobuf over TCP/IP" as the server's protocol, and build the library with
``KRPC_COMMUNICATION_TCP``. :func:`krpc_open` then takes a :type:`krpc_connection_config_t` with
the address and port of the server. The following connects to a server with the default settings,
address ``127.0.0.1`` and port 50000, and prints its version:

.. literalinclude:: /scripts/cnano/client/ConnectingTcp.c

The address and port match the ones shown in the server window. TCP/IP works over a network, so
your program can run on a different machine from the game.

Over a Local Socket
^^^^^^^^^^^^^^^^^^^

Select "Protobuf over local socket" as the server's protocol, and build the library with
``KRPC_COMMUNICATION_LOCALSOCKET``. :func:`krpc_open` then takes the path of the server's socket,
shown in the server window. The following connects to a server, and prints its version:

.. literalinclude:: /scripts/cnano/client/ConnectingLocal.c

A local socket works only on the machine running the game. It has lower overhead than TCP/IP, so
it is faster.

Closing the Connection
^^^^^^^^^^^^^^^^^^^^^^

:func:`krpc_disconnect` ends the session and closes the connection, as in the examples above. On
a serial port, this is how the server learns the session has ended.

Calling Remote Procedures
-------------------------

The server provides *procedures* for your program to call, grouped into *services*.

The following gets the active vessel from :func:`krpc_SpaceCenter_ActiveVessel`, an object of
type :type:`krpc_SpaceCenter_Vessel_t`. It sets the vessel's name, then prints its altitude:

.. literalinclude:: /scripts/cnano/client/RemoteProcedures.c

Each service has a header, such as ``krpc_cnano/services/space_center.h`` for the :doc:`SpaceCenter
</cnano/api/space-center>` service. Each procedure is a function that takes the connection, then a
pointer for the result if it has one, then its arguments. It returns a :type:`krpc_error_t`.

A procedure that returns a string, bytes or a collection allocates memory for it on the heap.
Initialize a string to ``NULL``, bytes to :macro:`KRPC_BYTES_NULL` and a collection to
:macro:`KRPC_NULL_LIST`, :macro:`KRPC_NULL_SET` or :macro:`KRPC_NULL_DICTIONARY` before each
call. Free it with :func:`krpc_free`, :macro:`KRPC_FREE_BYTES`, :macro:`KRPC_FREE_LIST`,
:macro:`KRPC_FREE_SET` or :macro:`KRPC_FREE_DICTIONARY` when you are done with it. Free the
strings and collections a collection holds before freeing the collection.

Handling Errors
---------------

Each function returns an error code, which is :macro:`KRPC_OK` on success. The following checks
the error from loading a save that does not exist:

.. literalinclude:: /scripts/cnano/client/Errors.c

:func:`krpc_get_error` describes an error code. The code depends on the error:

* A procedure that fails on the server returns :macro:`KRPC_ERROR_RPC_FAILED`. With
  ``KRPC_ERROR_MESSAGES`` defined, :func:`krpc_get_error_message` returns the server's message,
  which names the exception, such as ``KRPC.ArgumentException``.
* A connection that fails to open returns :macro:`KRPC_ERROR_IO`.
* A server that denies the connection, or does not support the client's protocol version, makes
  :func:`krpc_connect` return :macro:`KRPC_ERROR_CONNECTION_REFUSED`.
* A failed read or write returns :macro:`KRPC_ERROR_IO` or :macro:`KRPC_ERROR_EOF`.
* A call made after :func:`krpc_disconnect`, after a failed write, or after the server sends a
  disconnect, returns :macro:`KRPC_ERROR_CONNECTION_FAILED`.

The :ref:`compilation options <cnano-client-compilation-options>` can instead exit, fail an
assertion or call a handler on each error from a procedure. :func:`krpc_open`,
:func:`krpc_connect` and :func:`krpc_disconnect` always return the error code.

Remote Objects
--------------

Many procedures return an object identifying something in the game, such as a vessel or a part.
The object holds no copy of the game's state. It is just a reference to it, an identifier of type
:type:`krpc_object_t`. Each call through it reads the game as it is now:

.. literalinclude:: /scripts/cnano/client/RemoteObjects.c

Two objects for the same thing in the game compare equal with ``==``. :macro:`KRPC_NULL` is the
null object.

Once the thing an object identifies is destroyed, such as a part that explodes, each call through
it fails with ``KRPC.ObjectDestroyedException``, and returns :macro:`KRPC_ERROR_RPC_FAILED`.

An object for a vessel or a part keeps working when you load a save, quickload or revert, and
then reads the loaded game. Other objects depend on what they identify. A maneuver node, for
example, is destroyed by a load.

A vessel far from the active vessel is *unloaded*, and the game keeps no parts for it. The vessel
itself reads normally, but a call on one of its parts fails with
``KRPC.ObjectNotLoadedException``. The part works again once the vessel comes back into range.

.. seealso:: :doc:`/user-guide/object-lifetime` describes how each kind of object behaves in more
             detail.

Streams and Events
------------------

The C-nano client has no streams or events. Read a value by calling its procedure in a loop
instead.

Deprecated Members
------------------

A member of a service can be deprecated, when a newer member replaces it. A deprecated member
still works, but may be removed in a future release, so move to its replacement.

The API reference marks it as deprecated, and names the replacement. The compiler warns when your
program calls it, naming the replacement too.

Service Stubs
-------------

The client includes code for each service that ships with kRPC, generated from the services of
one kRPC release. These *stubs* are the headers in ``krpc_cnano/services``, such as
``krpc_cnano/services/space_center.h``.

The stubs match the server from the same release, so install the client version that matches your
server. The stubs call each procedure by a number, and another version can give that number to a
different procedure. A call can then run the wrong procedure, or send or return a wrong value, with
no error.

A service with no stub, such as one added by another mod, needs stubs of its own.
:ref:`service-api-clientgen` shows how to generate them.

.. _cnano-client-reference:

Client API Reference
--------------------

.. function:: krpc_error_t krpc_open(krpc_connection_t * connection, const krpc_connection_config_t * arg)

   Opens a connection, and writes it into *connection*. What *arg* holds depends on the protocol
   the library is built for:

   * ``KRPC_COMMUNICATION_POSIX`` and ``KRPC_COMMUNICATION_WINDOWS``: the name of the serial port,
     as a ``const char *``. For example, ``krpc_open(&conn, "/dev/ttyS0")`` or
     ``krpc_open(&conn, "COM3")``. The port keeps its baud rate and framing, so set them to match
     the server window, such as with ``stty`` on Linux.
   * ``KRPC_COMMUNICATION_TCP``: a pointer to a :type:`krpc_connection_config_t` holding the
     address and port of the server. The address is a host name or an IP address, and each
     endpoint it resolves to is tried in turn.
   * ``KRPC_COMMUNICATION_LOCALSOCKET``: the path of the server's socket, as a ``const char *``.
   * ``KRPC_COMMUNICATION_ARDUINO``: *connection* points to a ``HardwareSerial *``, such as
     ``&Serial``, set before the call. *arg* is a pointer to a :type:`krpc_connection_config_t`
     holding the baud rate and the data, parity and stop bits, or ``NULL`` for 9600 baud and
     ``SERIAL_8N1``.

   Returns :macro:`KRPC_ERROR_IO` if the connection fails to open, such as for an address that
   does not resolve or a socket path that is too long. On Arduino it returns :macro:`KRPC_OK`.

   .. note::

      A serial port has no end-of-file, so on Arduino a read fails with :macro:`KRPC_ERROR_EOF`
      when the serial timeout elapses without a single byte arriving. The timeout is one second
      by default and applies to each byte. Raise it with ``Serial.setTimeout`` before calling
      :func:`krpc_open` if the server can take longer to reply.

.. type:: krpc_connection_t

   The connection that :func:`krpc_open` opens and every call takes. It is a file descriptor on
   POSIX, a ``HANDLE`` on Windows, a ``HardwareSerial *`` on Arduino, and a socket over TCP/IP or
   a local socket. A ``KRPC_COMMUNICATION_CUSTOM`` build defines it, and
   :type:`krpc_connection_config_t`, before it includes ``krpc_cnano.h``.

.. type:: krpc_connection_config_t

   The configuration :func:`krpc_open` takes. For ``KRPC_COMMUNICATION_TCP``, a structure with the
   fields ``const char * address`` and ``uint16_t port``. For ``KRPC_COMMUNICATION_ARDUINO``, a
   structure with the fields ``uint32_t speed``, the baud rate, and ``uint8_t config``, such as
   ``SERIAL_8N1``. For a serial port or a local socket, ``char``, so *arg* is a string.

.. function:: krpc_error_t krpc_connect(krpc_connection_t connection, const char * name)

   Starts a session on a connection opened with :func:`krpc_open`, and waits for the server to
   accept it. The client supports one session at a time.

   :param connection: The connection.
   :param name: A name for the connection, shown in the server window in the game.

   Returns :macro:`KRPC_ERROR_CONNECTION_REFUSED`, and closes the connection, if the server
   denies it or does not support the client's protocol version. Over TCP/IP or a local socket,
   any other failure closes the connection too.

   On a serial port or a custom transport, part of a message from an earlier session can garble
   the connection request or its response. This then returns
   :macro:`KRPC_ERROR_CONNECTION_FAILED` and leaves the port open. Wait more than 3 seconds, so
   that the server drops the partial message, then call this again.

.. function:: krpc_error_t krpc_disconnect(krpc_connection_t connection)

   Tells the server the session is ending, then closes the connection.

.. function:: krpc_error_t krpc_close(krpc_connection_t connection)

   Closes the connection, without sending the server a disconnect. A
   ``KRPC_COMMUNICATION_CUSTOM`` build provides it, as it does :func:`krpc_open`.

.. type:: krpc_error_t

   The error code every function returns.

   .. macro:: KRPC_OK

              The function completed successfully.

   .. macro:: KRPC_ERROR_IO

              Opening, reading from or writing to the connection failed.

   .. macro:: KRPC_ERROR_EOF

              The connection ended while reading from it. On Arduino, no byte arrived within the
              serial timeout.

   .. macro:: KRPC_ERROR_CONNECTION_FAILED

              The session failed to start, or has ended, or the server sent a message that failed
              to decode or was not expected.

   .. macro:: KRPC_ERROR_NO_RESULTS

              The response from the server did not hold a single result.

   .. macro:: KRPC_ERROR_RPC_FAILED

              The procedure failed on the server.

   .. macro:: KRPC_ERROR_ENCODING_FAILED

              A message failed to encode or to send.

   .. macro:: KRPC_ERROR_DECODING_FAILED

              The return value from the server failed to decode.

   .. macro:: KRPC_ERROR_BUFFER_TOO_SMALL

              A value did not fit in the buffer given to hold it.

   .. macro:: KRPC_ERROR_CONNECTION_REFUSED

              The server denied the connection, or does not support the client's protocol
              version.

.. function:: const char * krpc_get_error(krpc_error_t error)

   A short description of an error code, such as ``rpc failed``. Returns an empty string for
   :macro:`KRPC_OK`.

.. function:: const char * krpc_get_error_message(void)

   The message of the most recent error from the server, or an empty string if there has not been
   one. It names the exception and gives its description, as in
   ``SpaceCenter.InvalidOperationException: Vessel does not exist``. The server stack trace
   follows when the server is configured to send one. The message is truncated to fit a buffer of
   ``KRPC_ERROR_MESSAGE_LENGTH`` bytes.

   Only available when the library is built with ``KRPC_ERROR_MESSAGES`` defined.

.. var:: void (*krpc_error_handler)(krpc_error_t)

   The function a procedure calls on an error, when the library is built with
   ``KRPC_ERROR_CHECK_FN``. Set it before the first call.

.. type:: krpc_object_t

   A remote object, as the identifier of the object on the server. Each class of a service is a
   ``typedef`` of it, such as :type:`krpc_SpaceCenter_Vessel_t`.

.. macro:: KRPC_NULL

   The null object.

.. function:: void krpc_free(void * ptr)

   Frees memory allocated on the heap, such as a string a procedure returned.

.. type:: krpc_bytes_t

   A byte array, with the fields ``size_t size`` and ``uint8_t * data``.

.. macro:: KRPC_BYTES_NULL

   An empty byte array. A procedure that returns bytes into it allocates the data on the heap.

.. macro:: KRPC_BYTES(name, size)

   Declares a byte array *name*, backed by a local buffer of *size* bytes, for passing as an
   argument.

.. macro:: KRPC_BYTES_FROM_BUFFER(buffer, size)

   A byte array over an existing buffer, for passing as an argument.

.. macro:: KRPC_FREE_BYTES(b)

   Frees the data of a byte array allocated on the heap, and empties it.

.. macro:: KRPC_NULL_LIST

   An empty list. Initialize a list to it before a procedure returns into it.
   :macro:`KRPC_NULL_SET` and :macro:`KRPC_NULL_DICTIONARY` do the same for a set and a
   dictionary.

.. macro:: KRPC_FREE_LIST(l)

   Frees the items of a list, and empties it. The memory the items hold, such as strings, is not
   freed. :macro:`KRPC_FREE_SET` and :macro:`KRPC_FREE_DICTIONARY` do the same for a set and a
   dictionary.

.. function:: void * krpc_malloc(size_t size)

   Allocates *size* bytes on the heap. The client allocates the strings, bytes and collections it
   decodes with these functions. Define ``KRPC_CUSTOM_MEMORY_ALLOC`` to provide your own
   :func:`krpc_malloc`, :func:`krpc_calloc`, :func:`krpc_recalloc` and :func:`krpc_free`.

.. function:: void * krpc_calloc(size_t num, size_t size)

   Allocates an array of *num* elements of *size* bytes on the heap, and zeroes it.

.. function:: void * krpc_recalloc(void * ptr, size_t num, size_t inc, size_t size)

   Grows the array *ptr* from *num* to *num* + *inc* elements of *size* bytes, and zeroes the new
   elements. *inc* must be greater than zero.

.. function:: krpc_error_t krpc_read(krpc_connection_t connection, uint8_t * buf, size_t count)

   Reads exactly *count* bytes from the connection into *buf*, waiting until they arrive. Returns
   :macro:`KRPC_ERROR_EOF` if the connection ends first, or :macro:`KRPC_ERROR_IO` if the read
   fails. A ``KRPC_COMMUNICATION_CUSTOM`` build provides it.

.. function:: krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t * buf, size_t count)

   Writes all *count* bytes from *buf* to the connection. Returns :macro:`KRPC_ERROR_IO` if the
   write fails. A ``KRPC_COMMUNICATION_CUSTOM`` build provides it.
