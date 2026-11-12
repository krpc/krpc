.. default-domain:: c
.. highlight:: c

C-nano
======

This guide describes how to install the C-nano client, and the features it provides. The
:ref:`cnano-client-reference` describes each function and type.

The client is written for microcontrollers and other devices with little memory, hence the "nano"
in its name. For the full feature set from C, use the :doc:`C++ client </cpp>`.

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
   they are built with the C++ compiler. The library uses the C++ ``Stream`` class to
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

A project in manifest mode lists the client in its ``vcpkg.json`` instead, and vcpkg installs it
when you configure the project:

.. code-block:: json

   {
     "dependencies": ["krpc-cnano"]
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

On Windows, also link ``ws2_32``, the winsock library the TCP/IP and local socket transports
use.

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
  * ``KRPC_ERROR_CODES_ONLY`` keeps only the code of each error. By default the library also
    keeps the details of the last error, for :func:`krpc_get_error_info`, in 16 bytes of RAM on
    AVR.
  * ``KRPC_ERROR_MESSAGES`` also keeps the message of the last error, for
    :func:`krpc_get_error_message`. It costs ``KRPC_ERROR_MESSAGE_LENGTH`` bytes of static
    storage, and the flash of each message.
  * ``KRPC_ERROR_MESSAGE_LENGTH`` is the size of the buffer for that message, 256 bytes by
    default.
  * ``PB_NO_ERRMSG`` disables the error messages of the nanopb library, which kRPC uses to encode
    and decode messages. The Arduino version of the library defines it by default.

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

The client connects over a serial port, TCP/IP, a local socket or a transport of your own. Each
transport has a function that opens a connection over it. Each server in the server window uses
one protocol, and accepts connections over that protocol only. Pick the protocol when you add the
server, and open the matching transport.

The library includes the serial port, TCP/IP and local socket transports on Windows, Linux and
macOS, and a serial port transport on Arduino. On any other platform, such as a microcontroller
outside Arduino, it includes none, and you supply your own.

Over a Serial Port
^^^^^^^^^^^^^^^^^^

Select "Protobuf over SerialIO" as the server's protocol. The server window also sets the port
name and the baud rate. The following connects to a server, and prints its version:

.. literalinclude:: /scripts/cnano/ConnectingSerial.c

:func:`krpc_open_serial` opens the serial port named by its second argument, such as ``COM3`` on
Windows or ``/dev/ttyS0`` on Linux. It writes the connection into its first argument. You pass the
connection to every call. :func:`krpc_connect` then starts a session, with a name for the
connection that appears in the server window in the game. Both return an error code, and the
example ends on a failure.

On an Arduino, :func:`krpc_open_serial` takes a serial port, such as ``&Serial``, and its baud
rate:

.. literalinclude:: /scripts/cnano/ConnectingArduino.ino
   :language: c

A client that resets can find part of a message from its earlier session on the line. The
example retries :func:`krpc_connect` until the server drops the partial message.

For framing other than 8 data bits, no parity and 1 stop bit, start the port in your sketch, then
open the connection with :func:`krpc_open_stream`:

.. code-block:: c

   Serial1.begin(115200, SERIAL_8E1);
   krpc_open_stream(&conn, &Serial1);

.. note::

   A serial port carries data far more slowly than the game produces it. The server drops a
   connection that sends more data than the port can carry.
   :ref:`communication-protocol-serialio-buffering` describes the limits.

Over TCP/IP
^^^^^^^^^^^

Select "Protobuf over TCP/IP" as the server's protocol. :func:`krpc_open_tcp` takes the address
and port of the server. The following connects to a server with the default settings, address
``127.0.0.1`` and port 50000, and prints its version:

.. literalinclude:: /scripts/cnano/ConnectingTcp.c

The address and port match the ones shown in the server window. TCP/IP works over a network, so
your program can run on a different machine from the game.

Over a Local Socket
^^^^^^^^^^^^^^^^^^^

Select "Protobuf over local socket" as the server's protocol. :func:`krpc_open_localsocket` takes
the path of the server's socket, shown in the server window. The following connects to a server,
and prints its version:

.. literalinclude:: /scripts/cnano/ConnectingLocal.c

A local socket works only on the machine running the game. It has lower overhead than TCP/IP, so
it is faster.

Over a Custom Transport
^^^^^^^^^^^^^^^^^^^^^^^

A :type:`krpc_transport_t` holds the functions that read, write and close a connection. Fill one
in to connect over a link the library has no transport for, such as a UART on a microcontroller.
The following connects over the program's standard input and output:

.. literalinclude:: /scripts/cnano/ConnectingCustom.c

The connection holds the transport and a handle, which is passed to each of its functions. The
handle is any integer or pointer, such as a pointer to your own state for the link.

Closing the Connection
^^^^^^^^^^^^^^^^^^^^^^

:func:`krpc_disconnect` ends the session and closes the connection, as in the examples above. On
a serial port, this is how the server learns the session has ended.

Calling Remote Procedures
-------------------------

The server provides *procedures* for your program to call, grouped into *services*.

The following gets the active vessel from :func:`krpc_SpaceCenter_ActiveVessel`, an object of
type :type:`krpc_SpaceCenter_Vessel_t`. It sets the vessel's name, then prints its altitude:

.. literalinclude:: /scripts/cnano/RemoteProcedures.c

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
for the exception the server throws when a save does not exist:

.. literalinclude:: /scripts/cnano/Errors.c

The code is the kind of error, and :func:`krpc_get_error` names it:

* :macro:`KRPC_ERROR_CONNECTION`: connecting failed, or the connection failed or ended. Each call
  after it fails the same way.
* :macro:`KRPC_ERROR_CONNECTION_REFUSED`: the server denied the connection, or does not support
  the client's protocol version.
* :macro:`KRPC_ERROR_SERVER`: the procedure failed on the server.
* :macro:`KRPC_ERROR_ENCODING`: an argument or a result failed to encode or decode.
* :macro:`KRPC_ERROR_ARGUMENT`: the library rejected an argument before sending anything, such
  as a null string.
* :macro:`KRPC_ERROR_NO_MEMORY`: an allocation failed.
* :macro:`KRPC_ERROR_BUFFER_TOO_SMALL`: bytes did not fit in the buffer you passed.

:func:`krpc_get_error_info` returns the details of the last error. A server error carries the
ids of its service and exception. :macro:`KRPC_IS_EXCEPTION` compares them with the
``KRPC_EXCEPTION_`` macro of an exception, such as
:macro:`KRPC_EXCEPTION_KRPC_ARGUMENTEXCEPTION`. A refused connection carries the server's status,
and a connection the server ended carries its reason.

With ``KRPC_ERROR_MESSAGES`` defined, :func:`krpc_get_error_message` returns the message of the
last error. A server error's message names the exception, as in
``KRPC.ArgumentException: Failed to load missing``.

The :ref:`compilation options <cnano-client-compilation-options>` can instead exit, fail an
assertion or call a handler on each error from a procedure. The functions that open a connection,
:func:`krpc_connect` and :func:`krpc_disconnect` always return the error code.

.. _cnano-client-values:

Values
------

Procedures take and return values of these types:

.. list-table::
   :header-rows: 1

   * - Type
     - C type
   * - Integer
     - ``int32_t``, ``int64_t``, ``uint32_t``, ``uint64_t``
   * - Floating point number, boolean
     - ``float``, ``double``, ``bool``
   * - String, bytes
     - ``char *``, :type:`krpc_bytes_t`
   * - Tuple, list, set, dictionary
     - A generated ``struct``, such as ``krpc_list_string_t``
   * - Class
     - A remote object, described below
   * - Enumeration
     - A ``typedef enum``
   * - Structure
     - A generated ``struct``
   * - Null
     - ``NULL`` for an argument, and a ``bool`` set for a result

.. _cnano-client-flags:

Flags Enumerations
^^^^^^^^^^^^^^^^^^

A flags enumeration, such as :type:`krpc_SpaceCenter_MapFilterType_t`, holds any combination of
its values. You combine values with ``|``, and test for one with ``&``:

.. literalinclude:: /scripts/cnano/Flags.c
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

Remote Objects
--------------

Many procedures return an object identifying something in the game, such as a vessel or a part.
The object holds no copy of the game's state. It is just a reference to it, an identifier of type
:type:`krpc_object_t`. Each call through it reads the game as it is now:

.. literalinclude:: /scripts/cnano/RemoteObjects.c

Two objects for the same thing in the game compare equal with ``==``. :macro:`KRPC_NULL` is the
null object.

Once the thing an object identifies is destroyed, such as a part that explodes, each call through
it fails with :macro:`KRPC_EXCEPTION_KRPC_OBJECTDESTROYEDEXCEPTION`, and returns
:macro:`KRPC_ERROR_SERVER`.

An object for a vessel or a part keeps working when you load a save, quickload or revert, and
then reads the loaded game. Other objects depend on what they identify. A maneuver node, for
example, is destroyed by a load.

A vessel far from the active vessel is *unloaded*, and the game keeps no parts for it. The vessel
itself reads normally, but a call on one of its parts fails with
``KRPC.ObjectNotLoadedException``. The part works again once the vessel comes back into range.

.. seealso:: :doc:`/user-guide/object-lifetime` describes how each kind of object behaves in more
             detail.

Calling Methods
---------------

Each method of a class has a function, which takes the connection first, as every function does.
A method's function then takes the object, followed by the method's arguments. A *static*
method needs no object, so its function takes the method's arguments straight after the
connection:

.. literalinclude:: /scripts/cnano/StaticMethods.c

Server Side Functions
---------------------

A server side function runs inside the game. You build it in your program with the expression
API, and the client sends it to the server, which runs it there. The following reads the vessel's
altitude, speed and vertical speed together:

.. literalinclude:: /scripts/cnano/RunFunction.c

Each procedure has a function with ``_call`` added to its name, such as
``krpc_SpaceCenter_Flight_Speed_call``. It fills in the call as a message, for
:func:`krpc_KRPC_Expression_Call` to embed in the function. The message holds the encoded
arguments on the heap, so free it with :func:`krpc_free_procedure_call`.
:ref:`server-side-functions-api` in the :ref:`server side functions guide
<server-side-functions>` describes how to build functions.

:func:`krpc_run_function` runs a function once, within one physics tick. Its arguments and its
value are encoded bytes. Encode each argument at the type of its parameter with the
``krpc_encode_`` functions, and decode the value at the function's type with the
``krpc_decode_`` functions.

A function takes arguments through its parameters. The following compiles a function ahead of its
first run, then passes it an argument:

.. literalinclude:: /scripts/cnano/RunFunctionArguments.c

:func:`krpc_compile_function` makes the first run as fast as later ones.

A server side function names a class, an enumeration, a structure or an exception by its ids.
Each has a macro holding the pair, such as :macro:`KRPC_CLASS_SPACECENTER_ENGINE`, which the
``ById`` procedures take:

.. code-block:: c

   krpc_KRPC_Type_ClassTypeById(conn, &engine_type, KRPC_CLASS_SPACECENTER_ENGINE);

:func:`krpc_KRPC_Expression_GetFieldByIndex` reads a field by its position in the structure.
Each field has a macro holding its position, such as :macro:`KRPC_FIELD_SPACECENTER_COMMNODE_NAME`.

Streams and Events
------------------

The C-nano client has no streams or events. Read a value by calling its procedure in a loop
instead.

Deprecated Members
------------------

A member of a service can be deprecated, when a newer member replaces it. A deprecated member
still works, but may be removed in a future release, so move to its replacement.

The API reference marks it as deprecated, and names the replacement. The compiler warns when your
program uses it, naming the replacement too.

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

.. function:: krpc_error_t krpc_open_serial(krpc_connection_t * connection, const char * port)

   Opens the serial port named *port*, such as ``/dev/ttyS0`` or ``COM3``, and writes the
   connection into *connection*. The port keeps its baud rate and framing, so set them to match
   the server window, such as with ``stty`` on Linux.

   On Arduino, it takes a serial port and a baud rate instead:
   ``krpc_open_serial(&conn, &Serial, 9600)``. It starts the port with 8 data bits, no parity and
   1 stop bit, waits for the port to be ready, then opens a connection over it with
   :func:`krpc_open_stream`.

   Returns :macro:`KRPC_ERROR_CONNECTION` if the port fails to open.

.. function:: krpc_error_t krpc_open_stream(krpc_connection_t * connection, Stream * stream)

   Opens a connection over an Arduino ``Stream`` that the sketch has started, such as a serial
   port after ``begin``. Closing the connection leaves the stream open. Arduino only.

   .. note::

      A serial port has no end-of-file, so on Arduino a read fails with :macro:`KRPC_ERROR_EOF`
      when the stream timeout elapses without a single byte arriving. The timeout is one second
      by default and applies to each byte. Raise it with ``setTimeout`` on the port if the server
      can take longer to reply.

.. function:: krpc_error_t krpc_open_tcp(krpc_connection_t * connection, const char * address, uint16_t port)

   Opens a TCP/IP connection to the server at *address* and *port*, and writes it into
   *connection*. The address is a host name or an IP address, and each endpoint it resolves to is
   tried in turn. Windows, Linux and macOS only.

   Returns :macro:`KRPC_ERROR_CONNECTION` if the address does not resolve or no endpoint accepts
   the connection.

.. function:: krpc_error_t krpc_open_localsocket(krpc_connection_t * connection, const char * path)

   Opens a connection to the server's local socket at *path*, and writes it into *connection*.
   Windows, Linux and macOS only. On Windows, it needs Windows 10 version 1803 or later, and a
   Windows SDK with ``afunix.h``, 10.0.17134 or later. With an older SDK the library leaves it
   out, and a call to it fails to compile.

   Returns :macro:`KRPC_ERROR_ARGUMENT` if the path is too long, and
   :macro:`KRPC_ERROR_CONNECTION` if the connection fails.

.. type:: krpc_connection_t

   A connection, which every call takes. A structure with the fields
   ``const krpc_transport_t * transport`` and ``uintptr_t handle``. The handle is passed to each
   of the transport's functions.

.. type:: krpc_transport_t

   A transport, as the functions that carry a connection's bytes. A structure with the fields:

   * ``krpc_error_t (*read)(uintptr_t handle, uint8_t * buf, size_t count)`` reads exactly *count*
     bytes into *buf*. It returns :macro:`KRPC_ERROR_EOF` once the connection has ended, and
     :macro:`KRPC_ERROR_IO` if the read fails.
   * ``krpc_error_t (*write)(uintptr_t handle, const uint8_t * buf, size_t count)`` writes all
     *count* bytes from *buf*. It returns :macro:`KRPC_ERROR_IO` if the write fails.
   * ``krpc_error_t (*close)(uintptr_t handle)`` closes the connection. ``NULL`` for a transport
     with nothing to close.
   * ``bool resumable`` is whether the link outlives a session, as a serial line does. A client
     that resets can then find part of a message from its earlier session on the link, and
     :func:`krpc_connect` leaves the connection open after a failed handshake.

   Before a function returns :macro:`KRPC_ERROR_IO` or :macro:`KRPC_ERROR_EOF`, it can give the
   failure's message with :func:`krpc_set_error_cause`. The call that failed returns
   :macro:`KRPC_ERROR_CONNECTION`, with the transport's code as the cause.

.. var:: const krpc_transport_t krpc_transport_serial

   The serial port transport. Its handle is a file descriptor, or a ``HANDLE`` on Windows.

.. var:: const krpc_transport_t krpc_transport_socket

   The TCP/IP and local socket transport. Its handle is a file descriptor, or a ``SOCKET`` on
   Windows.

.. var:: const krpc_transport_t krpc_transport_stream

   The Arduino ``Stream`` transport. Its handle is a ``Stream *``.

.. function:: krpc_error_t krpc_connect(krpc_connection_t connection, const char * name)

   Starts a session on an open connection, and waits for the server to accept it. The client
   supports one session at a time.

   :param connection: The connection.
   :param name: A name for the connection, shown in the server window in the game.

   Returns :macro:`KRPC_ERROR_CONNECTION_REFUSED`, and closes the connection, if the server
   denies it or does not support the client's protocol version. Returns
   :macro:`KRPC_ERROR_CONNECTION` if the connection is not open or the handshake fails. The last
   error holds the server's status and the reason of a disconnect. Over TCP/IP, a local socket,
   or any transport with ``resumable`` false, a failed handshake closes the connection too.

   On a resumable transport, such as a serial port, part of a message from an earlier session can
   garble the connection request or its response. This returns :macro:`KRPC_ERROR_CONNECTION` and
   leaves the port open. Wait more than 3 seconds, so that the server drops the partial message,
   then call this again.

.. function:: krpc_error_t krpc_disconnect(krpc_connection_t connection)

   Tells the server the session is ending, then closes the connection. Returns
   :macro:`KRPC_ERROR_CONNECTION` if the connection is not open or fails to close.

.. function:: krpc_error_t krpc_close(krpc_connection_t connection)

   Closes the connection, without sending the server a disconnect.

.. type:: krpc_error_t

   The error code every function returns.

   .. macro:: KRPC_OK

              The function completed successfully.

   .. macro:: KRPC_ERROR_CONNECTION

              Connecting failed, or the connection failed or ended.

   .. macro:: KRPC_ERROR_CONNECTION_REFUSED

              The server denied the connection, or does not support the client's protocol
              version.

   .. macro:: KRPC_ERROR_SERVER

              The procedure failed on the server.

   .. macro:: KRPC_ERROR_ENCODING

              An argument or a result failed to encode or decode.

   .. macro:: KRPC_ERROR_ARGUMENT

              The library rejected an argument before sending anything.

   .. macro:: KRPC_ERROR_NO_MEMORY

              An allocation failed.

   .. macro:: KRPC_ERROR_BUFFER_TOO_SMALL

              A value did not fit in the buffer given to hold it.

   .. macro:: KRPC_ERROR_IO

              A transport's read or write failed. The call that failed returns
              :macro:`KRPC_ERROR_CONNECTION`.

   .. macro:: KRPC_ERROR_EOF

              A transport's connection ended. On Arduino, no byte arrived within the serial
              timeout. The call that failed returns :macro:`KRPC_ERROR_CONNECTION`.

.. function:: const char * krpc_get_error(krpc_error_t error)

   The name of an error code, such as ``server error``.

.. type:: krpc_error_info_t

   The details of the last error. A structure with the fields:

   * ``krpc_error_t code``, the code the failing function returned.
   * ``krpc_error_t cause``, :macro:`KRPC_ERROR_IO` or :macro:`KRPC_ERROR_EOF` for the
     transport's failure behind a connection error, else -1.
   * ``uint32_t service`` and ``uint32_t exception``, the ids of a server error's exception, else
     0. A server error with no exception has 0 for both.
   * ``int16_t status``, the status of a ``ConnectionResponse`` other than ``OK``, else -1.
   * ``int16_t reason``, the ``Disconnect`` reason of a connection the server ended, else -1.

   A status or reason the protocol does not define keeps its number.

.. function:: const krpc_error_info_t * krpc_get_error_info(void)

   The details of the last error. Each failure fills them in, and a success leaves them as they
   were. Not available when the library is built with ``KRPC_ERROR_CODES_ONLY`` defined.

.. macro:: KRPC_IS_EXCEPTION(exception)

   Whether the last error is the server exception given by its ``KRPC_EXCEPTION_`` macro, such as
   ``KRPC_IS_EXCEPTION(KRPC_EXCEPTION_KRPC_ARGUMENTEXCEPTION)``. Not available when the library is
   built with ``KRPC_ERROR_CODES_ONLY`` defined.

.. function:: bool krpc_is_exception(uint32_t service, uint32_t exception)

   Whether the last error is the server exception with the given service and exception ids.

.. function:: const char * krpc_get_error_message(void)

   The message of the last error, or an empty string if there has not been one. It is a sentence
   saying what failed, then the message of its cause after a colon, as in
   ``Connection failed: Connection reset by peer``. A server error names the
   exception and gives its description, as in
   ``SpaceCenter.InvalidOperationException: Vessel does not exist``. The server stack trace
   follows when the server is configured to send one. The message is cut to fit a buffer of
   ``KRPC_ERROR_MESSAGE_LENGTH`` bytes.

   Only available when the library is built with ``KRPC_ERROR_MESSAGES`` defined.

.. function:: void krpc_set_error_cause(const char * message)

   Gives the message of a transport's failure, before its function returns
   :macro:`KRPC_ERROR_IO` or :macro:`KRPC_ERROR_EOF`. The library puts it after its own message.
   Does nothing unless the library is built with ``KRPC_ERROR_MESSAGES`` defined.

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
   fails.

.. function:: krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t * buf, size_t count)

   Writes all *count* bytes from *buf* to the connection. Returns :macro:`KRPC_ERROR_IO` if the
   write fails.

.. function:: krpc_error_t krpc_compile_function(krpc_connection_t connection, krpc_KRPC_Expression_t function)

   Compiles a server side function on the server, so that its first run is as fast as later ones.
   Declared in ``krpc_cnano/functions.h``.

.. function:: krpc_error_t krpc_run_function(krpc_connection_t connection, krpc_bytes_t * result, bool * isNull, krpc_KRPC_Expression_t function, const krpc_list_nullable_bytes_t * arguments)

   Runs a server side function within a single physics tick. Declared in
   ``krpc_cnano/functions.h``.

   :param connection: The connection.
   :param result: The encoded value of the function, allocated on the heap. No bytes for a
                  function with no value. Pass it empty, from :macro:`KRPC_BYTES_NULL` or
                  :macro:`KRPC_FREE_BYTES`, as a null value or a shorter value leaves bytes it
                  already holds in place.
   :param isNull: Set to whether the value is null.
   :param function: The function to run.
   :param arguments: The encoded arguments, one per parameter, with ``has_value`` false for a
                     null. ``NULL`` for a function with no parameters.

.. function:: krpc_error_t krpc_free_procedure_call(krpc_schema_ProcedureCall * call)

   Frees the encoded arguments of a call that a ``_call`` function filled in. Free a call that
   failed to fill in too, and free a call before filling it in again.

API Reference
-------------

.. toctree::
   :maxdepth: 1

   cnano/api/krpc
   cnano/api/stdlib
   cnano/api/space-center
   cnano/api/drawing
   cnano/api/ui
   cnano/api/infernal-robotics
   cnano/api/kerbal-alarm-clock
   cnano/api/remote-tech
   cnano/api/lidar
   cnano/api/docking-camera
   cnano/api/trajectories
   cnano/api/mechjeb
   cnano/api/kerbal-engineer-redux
   cnano/api/scansat
   cnano/api/debug
