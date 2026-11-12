C-nano Client Design
====================

The internal design of the C-nano client. This page builds on the :doc:`shared client design
</client-design>` and the :doc:`client specification </client-spec>`. Usage is in
the :doc:`C-nano guide </cnano>`. Paths are relative to ``client/cnano/``.

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

Errors
------

Library functions return a ``krpc_error_t``, one code per kind of error a program handles
differently. A refusal has a code of its own, apart from the other connection errors, as a
program retries a garbled handshake on a serial line and gives up on a refusal.

``KRPC_CHECK`` applies only in the stubs and ``functions.h``, where ``KRPC_ERROR_CHECK_ASSERT``,
``KRPC_ERROR_CHECK_EXIT`` and ``KRPC_ERROR_CHECK_FN`` replace its ``return``.

* **Transports keep their own codes.** A transport's functions, ``krpc_read`` and ``krpc_write``
  return ``KRPC_ERROR_IO`` and ``KRPC_ERROR_EOF``. A library function turns either into
  ``KRPC_ERROR_CONNECTION``, with the transport's code as the cause, so custom transports stay as
  they are.
* **One record holds the last error**, as ``errno`` does. The client runs one session at a time on
  the calling thread, so one static record serves. A field that does not apply is -1, as
  ``KRPC_OK``, the status ``OK`` and the reason ``CLOSED`` are real values. The ids are 0, which is
  never an id.
* **A server exception is told apart by its ids.** Comparing the names would put them in flash.
  The stubs define ``KRPC_EXCEPTION_<SERVICE>_<NAME>`` as the pair, which ``KRPC_IS_EXCEPTION``
  compares with the record.
* **One macro fills the record at each failure site.** ``KRPC_FAIL`` takes a sentence, compiled only
  when the library keeps or prints messages. On AVR a kept sentence is a ``PSTR`` string, so it
  stays in flash.
* **A callback's failure keeps its code.** A nanopb callback returns only false, so it stores its
  code in ``krpc_callback_error``. The function that ran nanopb returns that code in place of
  ``KRPC_ERROR_ENCODING``. A null string argument thus returns ``KRPC_ERROR_ARGUMENT``.
* **A cause's message comes first and the sentence goes in front of it.** A transport or the
  server gives the cause's message before the library knows the failure. ``krpc_fail`` then moves
  the message along to put the sentence and a colon in front, cutting what does not fit.

The detail levels trade memory for what the program can tell:

.. list-table::
   :header-rows: 1

   * - Define
     - Kept
     - Cost
   * - ``KRPC_ERROR_CODES_ONLY``
     - The returned code
     - None. A server error is skipped without parsing, so the ``Error`` descriptor is not linked
   * - None, the default
     - The last-error record
     - 16 bytes of RAM on AVR, and a few stores at each failure
   * - ``KRPC_ERROR_MESSAGES``
     - The record and the message
     - ``KRPC_ERROR_MESSAGE_LENGTH`` bytes of RAM, and each sentence in flash

The record is the default, as an Arduino sketch nearly always has its 16 bytes to spare.
``KRPC_ERROR_BUFFER_TOO_SMALL`` stays apart from ``KRPC_ERROR_ARGUMENT``, as a program retries
with a larger buffer.

A session ends on a failed read or write, a ``disconnect``, an unexpected message and a message that
fails to parse. A response with other than one result is an unexpected message. A session survives
a server error and a return value that fails to decode, as those responses have been read in full.
A result the client has no memory for is skipped to the end of its message, so the session survives
that too. A request that fails to encode ends the session only once part of it has been written.

A null result returns no error code, as null is a valid value and the code would trip
``KRPC_CHECK``.

``KRPC_CHECK`` and ``KRPC_ALLOC_BLOCK_SIZE`` are read when the program compiles the stubs.
``KRPC_BUFFER_SIZE``, the detail level and ``KRPC_CUSTOM_MEMORY_ALLOC`` are read when the library
compiles. ``KRPC_ERROR_CHECK_FN`` is read by both.

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
* ``krpc_connect`` closes on a refusal, and on any failure over a non-resumable transport. Over a
  resumable one, other failures keep the line open for the program to retry.

A session that fails during a call leaves the connection open. Later calls return
``KRPC_ERROR_CONNECTION`` without using it, and ``krpc_disconnect`` then only closes.

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

Mapping the Spec to C
---------------------

.. list-table::
   :header-rows: 1

   * - Spec
     - C
   * - Closing a connection
     - ``krpc_disconnect``, with no half-close or drain
   * - Connection error
     - ``KRPC_ERROR_CONNECTION``, and ``KRPC_ERROR_CONNECTION_REFUSED`` for a refusal, with the
       status, reason and cause in the last error
   * - Encoding error
     - ``KRPC_ERROR_ENCODING``
   * - Server error
     - ``KRPC_ERROR_SERVER``, with the exception's ids in the last error
   * - Argument error
     - ``KRPC_ERROR_ARGUMENT``, and ``KRPC_ERROR_BUFFER_TOO_SMALL``
   * - Error types
     - The codes, and a ``KRPC_EXCEPTION_`` macro per exception. The conventions name no remote
       error type, and a server error's message starts with ``Service.Name:``
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
