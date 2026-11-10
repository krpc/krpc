C-nano Client Design
====================

The internal design of the C-nano client. This page builds on the :doc:`shared client design
</client-design>` and the :doc:`client specification </client-spec>`. Usage is in
the :doc:`C-nano guide </cnano>`. Paths are relative to ``client/cnano/``.

Memory
------

nanopb runs without its malloc support, and every variable-length field is a callback. A
``krpc_schema_Message`` therefore has a fixed size and lives on the stack. All heap memory goes
through ``krpc_malloc``, ``krpc_calloc`` and ``krpc_recalloc``, which ``KRPC_CUSTOM_MEMORY_ALLOC``
replaces.

.. list-table::
   :header-rows: 1

   * - Memory
     - Allocated by
     - Released by
   * - Send and receive buffers, ``KRPC_BUFFER_SIZE``
     - Stack of ``krpc_send_message``, ``krpc_receive_message``
     - The function's return
   * - ``krpc_call_t``, ``krpc_argument_t[]``, ``krpc_result_t``
     - Stack of the stub
     - The stub's return
   * - A result's encoded value
     - ``krpc_malloc``, once per call with a value
     - ``krpc_free_result`` in the stub
   * - A returned string
     - ``krpc_malloc``, when the caller's pointer is ``NULL``
     - The caller, with ``krpc_free``
   * - Returned bytes
     - ``krpc_malloc``, when the caller's ``size`` is 0
     - The caller, with ``KRPC_FREE_BYTES``
   * - A returned collection
     - ``krpc_calloc``, grown by ``KRPC_ALLOC_BLOCK_SIZE`` items
     - The caller, with ``KRPC_FREE_*``
   * - An argument
     - The caller, borrowed for the call
     - The caller
   * - The encoded arguments of a ``_call`` function
     - ``krpc_calloc`` and ``krpc_malloc``
     - The caller, with ``krpc_free_procedure_call``

The caller's variable selects the allocation, so the caller resets it before each call. A struct or
tuple has no reset macro, so the caller zeroes it. ``KRPC_FREE_*`` frees one level.

A send encodes into its buffer with five bytes free in front, then writes the size prefix there,
which avoids a sizing pass. A message that overflows is measured and encoded again, one buffer at a
time. A receive asks the connection only for bytes of the current message, so a read never waits on
a message that has not been requested.

Ownership
---------

``krpc_connection_t`` is a value holding a transport pointer and a handle. A copy is the same
connection, and the program owns it. The transport table must outlive it. No reference count exists,
so a connection closes only as follows:

* ``krpc_disconnect`` sends a ``disconnect`` if the session is live, then closes.
* ``krpc_close`` closes with no message.
* ``krpc_connect`` closes on a refusal, a failed write, and any failure over a non-resumable
  transport. Over a resumable one, other failures keep the line open for the program to retry.

A session that fails during a call leaves the connection open. Later calls return
``KRPC_ERROR_CONNECTION_FAILED`` without using it, and ``krpc_disconnect`` then only closes.

Threads
-------

The client has no threads and no locks. Each call blocks its caller until the response arrives.
Transport functions, nanopb callbacks and ``krpc_error_handler`` run on the calling thread.

Session state is in file statics in ``src/krpc.c``: the next request id, whether the session has
ended and whether a response has been read yet. ``krpc_connect`` resets them, so the client supports
one open connection at a time. A program serializes all its calls, across connections too.

There is no timed read, so a POSIX or Windows call can block indefinitely. An Arduino read fails
after the ``Stream`` timeout with no byte, which ends the session, so a sketch does not spin once
the server goes away.

Transports
----------

A transport is a ``krpc_transport_t`` table, chosen at run time by the function that opens the
connection. The platform decides at compile time which tables exist:

.. list-table::
   :header-rows: 1

   * - Detected by
     - Openers
     - Table
   * - ``ARDUINO``
     - ``krpc_open_stream``, template ``krpc_open_serial``
     - ``krpc_transport_stream``
   * - ``_WIN32``, ``__unix__``, ``__APPLE__``
     - ``krpc_open_serial``, ``krpc_open_tcp``, ``krpc_open_localsocket``
     - ``krpc_transport_serial``, ``krpc_transport_socket``
   * - Anything else
     - None
     - The program's own

* The run-time choice lets one build carry every transport. The vcpkg port needs this, as vcpkg
  features must be additive.
* Each transport is one source file, so an unused one costs no code in a static link.
* ``resumable`` means the link outlives the client. ``krpc_connect`` then skips a leftover message,
  and a call skips a ``connection_response`` before its first response.
* The Arduino build is C++. ``krpc_open_serial`` is a template, as the serial classes of the cores
  share no base class with ``begin``.
* ``krpc_open_stream`` takes a ``Stream``, the base class of ``HardwareSerial``, ``SoftwareSerial``
  and USB ports.

Errors
------

Library functions return a ``krpc_error_t``. ``KRPC_CHECK`` applies only in the stubs and
``functions.h``, where ``KRPC_ERROR_CHECK_ASSERT``, ``KRPC_ERROR_CHECK_EXIT`` and
``KRPC_ERROR_CHECK_FN`` replace its ``return``.

A session ends on a failed read or write, a ``disconnect``, an unexpected message and a message that
fails to parse. It survives a server error and a return value that fails to decode, as those
responses have been read in full. A transport failing partway through a message reaches
``krpc_invoke`` as a decode failure, so the call returns ``KRPC_ERROR_CONNECTION_FAILED``.

A null result returns no error code, as null is a valid value and the code would trip
``KRPC_CHECK``.

``KRPC_CHECK`` and ``KRPC_ALLOC_BLOCK_SIZE`` are read when the program compiles the stubs.
``KRPC_BUFFER_SIZE``, ``KRPC_ERROR_MESSAGES`` and ``KRPC_CUSTOM_MEMORY_ALLOC`` are read when the
library compiles. ``KRPC_ERROR_CHECK_FN`` is read by both. ``KRPC_ERROR_MESSAGES`` is optional, as
its buffer costs static memory on a small target.

Service Stubs
-------------

``clientgen`` generates one header per service from ``cnano.tmpl`` in ``krpctools``. Each function
is ``static inline``, so a header can be included from several translation units.

A stub calls by service and procedure id, and ``src/krpc.options`` removes the name fields. This
keeps names out of flash and off the wire.

Each procedure also has a ``_call`` function, for server side functions. An unused one adds compile
time and no code.

``KRPC_DEPRECATED`` goes before a declaration, the one position GCC, Clang and MSVC share. An
enumeration value takes ``KRPC_DEPRECATED_VALUE`` after its name, as the attribute has no other
position there. MSVC has no such attribute in C, so the macro is empty for it.

Server Side Functions
---------------------

The stubs leave out internal procedures, so ``krpc_compile_function`` and ``krpc_run_function`` are
written by hand in ``include/krpc_cnano/functions.h``. They hold the ids of ``KRPC.CompileFunction``
and ``KRPC.RunFunction``. ``clientgen`` adds ``krpc_list_nullable_bytes_t`` to the ``KRPC`` header,
as no public procedure uses that type.

* **A run passes encoded bytes.** C has no generics, so ``krpc_run_function`` takes no type. The
  program encodes the arguments and decodes the value at the types it knows.
* **A null value has no bytes.** Decoding it fails at most types. It succeeds at a list or a tuple,
  which leaves the list empty and the tuple's items unset, so the program checks ``isNull`` there.
* **A call message copies its arguments.** A stub's ``krpc_call_t`` borrows its arguments for
  the call. A ``krpc_schema_ProcedureCall`` outlives the ``_call`` function that fills it in, so
  each argument is encoded onto the heap, tag and length included. Encoding the message then writes
  those bytes as they are.
* **That code has its own file**, ``src/procedure_call.c``, so a static link without functions
  leaves it out.
* **There are no function streams or events**, as the client has no streams.

Mapping the Spec to C
---------------------

.. list-table::
   :header-rows: 1

   * - Spec
     - C
   * - Closing a connection
     - ``krpc_disconnect``, with no half-close or drain
   * - Connection error
     - ``KRPC_ERROR_CONNECTION_FAILED``, ``_CONNECTION_REFUSED``, ``_IO``, ``_EOF``
   * - Encoding error
     - ``KRPC_ERROR_ENCODING_FAILED``, ``KRPC_ERROR_DECODING_FAILED``
   * - Server error
     - ``KRPC_ERROR_RPC_FAILED``, with a message only under ``KRPC_ERROR_MESSAGES``
   * - Null object
     - ``KRPC_NULL``, the id 0
   * - Nullable argument, result
     - ``const T*``, where ``NULL`` is null; a ``bool* returnValueIsNull``
   * - Nullable position in a value
     - A generated ``krpc_nullable_*_t``, the value beside ``has_value``
   * - Getting a call as a value
     - A generated ``_call`` function
   * - Value type of a server side function
     - The program decodes encoded bytes
   * - Handshake and stall timeouts
     - None, as the client sets no timeout of its own
   * - Resuming a serial session
     - The program calls ``krpc_connect`` again
   * - Double on AVR
     - A 32-bit ``double``, converted to and from 64 bits by ``src/utils.c``
   * - Flags enumeration
     - A ``typedef enum``, combined with C's integer operators
