Python Client Design
====================

The internal design of the Python client. This page builds on the :doc:`shared client design
</client-design>` and the :doc:`client specification </client-spec>`. Usage is in
the :doc:`Python guide </python>`. Paths are relative to ``client/python/``.

Ownership
---------

Services, objects and streams hold the ``Client`` strongly. Letting go of them closes nothing:

.. code-block:: python

   def active_vessel():
       conn = krpc.connect()
       return conn.space_center.active_vessel
       # The connection stays open after conn is dropped

References
^^^^^^^^^^

The client holds its ``Connection``, ``Session``, ``StreamManager``, ``Types`` registry and
services.

The objects the program keeps also hold the client. A ``Service``, ``Object`` or ``StreamImpl``
holds the ``Client``, and a ``Stream`` holds its ``StreamImpl``.

The threads keep the client alive. The reader and dispatcher threads hold the ``Session``, which
holds the ``StreamManager``, which holds the ``Client``. An unclosed client is therefore never
collected. The threads end on a close or a connection failure.

The stream manager holds each open ``StreamImpl`` strongly, so a stream outlives its handles and its
callbacks keep running. It holds every registered ``StreamImpl`` weakly, which keeps nothing alive.
Each ``StreamImpl`` also holds the ``StreamManager``.

The collector frees these cycles once the threads end. A callback holding its own stream or the
client needs no special handling.

Lifetime
^^^^^^^^

* The connection closes on ``close()``, at the end of a ``with`` block, when the server or the
  socket fails, or at process exit. Process exit sends no ``disconnect``, as the operating system
  closes the socket.
* After ``close()``, the client and its objects stay valid. Calls raise ``ConnectionError``, and
  streams and events end. A second ``close()`` has no further effect.
* Lifetime is explicit. ``Client``, objects, ``Stream`` and ``Event`` have no finalizer. An object
  is an id and a client, and sends no release to the server.
* ``Stream`` and ``Event`` are handles to one ``StreamImpl``, which runs until a ``remove()``
  through any handle, a server ending or the close. Both are context managers, whose exit calls
  ``remove()``. An exception from ``remove()`` is dropped when the block raised one.

Threads
-------

The threads follow the :doc:`shared design </client-design>`, and are named
``krpc-reader`` and ``krpc-dispatcher``.

* Calls and waits block the calling thread.
* A ``KeyboardInterrupt`` in a call abandons its slot.
* A value is decoded once, and every read and callback shares that object.
* A ``BaseException`` from an end callback, such as ``KeyboardInterrupt``, is raised once the close
  finishes.
* On the dispatcher thread, every callback exception goes to ``threading.excepthook``.
* During a close, another thread can hold a stream's condition. A ``krpc-waker`` thread then wakes
  the threads waiting on it, once the condition is free.

Services and Declarations
-------------------------

Every service is built at connect from ``GetServices``, by ``create_service``. The client then has
the members of the server it connects to, including those another mod adds to a shipped class.
Each client has its own classes, in its ``Types``, so a static method is a closure over its client.

* A connect registers the types of every service, then builds the services. The types come first as
  a procedure can use a type from any service. A parameter's default value can also be an
  enumeration value, which cannot be decoded until that enumeration is registered.
* ``clientgen`` and ``python.tmpl`` in ``krpctools`` generate a module per service. Its
  enumerations, structures and exceptions are real classes, and its classes and services are
  declarations whose members raise ``TypeError``. ``clientgen`` runs the client without the modules.
* The modules of the core's services are in ``krpc/services/``, and those of the KSP services in
  ``krpc/ksp/``. The paths match the planned split into a base package and a package per game, so
  the split changes no import. ``import krpc`` imports no module.
* A module registers its enumerations, structures and exceptions in ``krpc/declarations.py`` on
  import. A later registration replaces an earlier one.
* At connect, a client uses a generated class where the server's definition matches: an enumeration
  on its names and values, a structure on its field names in order, an exception on its name and
  the builtin it derives from. These classes hold no client, so every client shares them. A
  generated class is never changed to fit a server, as every client shares it.
* The client type picks the module of each service it declares, from the ``<Service>Client`` bases
  in its MRO, and the first wins. Any other service takes the registered classes. A module
  generated for a newer server then applies only to the connections typed with it.
* Otherwise the client builds its own class. A value of the module's enumeration converts to the
  built one by its number, as the other clients send it, since a client and a server of different
  releases are not supported. A structure decodes using the field types the server declares in
  either case. ``except`` compares exception classes by identity, so only a shared exception class
  catches a server error.
* A built class does not derive from its declaration, which would show members a server lacks.
  ``DeclaredClass``, the metaclass of a declaration, compares the service and class names for
  ``isinstance`` and ``issubclass``.
* Python has no declaration merging, so a module cannot add its service to ``Client``. It declares
  ``<Service>Client``, a subclass of ``Client`` with the service as an attribute. A program combines
  them, and ``connect[T]`` builds the client as ``T``. ``krpc.ksp.KSPClient`` combines the core
  and KSP services.
* ``Client`` declares no service, only a ``__getattr__`` returning ``Any`` for type checkers. The
  core services, ``KRPC`` and ``StdLib``, have client types like the others, so a module generated
  for a newer server replaces them the same way. A program with a client type that lacks a service
  then type checks, and a misspelled service is not reported.
* The client's own calls to ``KRPC`` go through ``Client._krpc``, a property typed with the shipped
  declaration.
* ``connect`` with no client type connects with ``KSPClient``, so a program written for KSP type
  checks with no client type. It imports ``krpc.ksp`` on the first connect, which keeps
  ``import krpc`` free of generated modules.

Server Side Functions
---------------------

``compile``, ``run``, ``add_stream`` and ``add_event`` take a Python function.
``krpc/functioncompiler.py`` and ``krpc/functionstatements.py`` compile its source, read with
``inspect`` and ``ast``, into a ``KRPC.Expression``.

A function passed directly to ``run`` or ``add_stream`` compiles on every call, as a cached function
would keep the values it first captured.

Transports
----------

``Connection`` is TCP/IP, and ``LocalConnection`` is a unix domain socket. They differ only in
``_open``, and there are no WebSockets or serial transports. Windows CPython has no
``socket.AF_UNIX``, so ``krpc/winsock.py`` opens that socket through Winsock directly.

``timeout`` bounds a TCP/IP connect only. ``handshake_timeout`` covers the whole handshake response.
``stall_timeout`` runs from the last byte received, while a message is part way through arriving.

Mapping the Spec to Python
--------------------------

.. list-table::
   :header-rows: 1

   * - Spec
     - Python
   * - Closing a connection
     - ``Client.close()``, or the end of a ``with`` block
   * - Connection, stream, encoding error
     - ``ConnectionError``, ``StreamError``, ``EncodingError`` in ``krpc.error``
   * - Server error with no mapped type
     - ``krpc.error.RPCError``
   * - Server exception with a mapped type
     - A class on the service, deriving from the builtin in ``types.NATIVE_EXCEPTIONS``
   * - Null object, nullable value
     - ``None``
   * - Member names
     - ``snake_case``, with ``_`` appended to a Python keyword
   * - Waiting
     - ``threading.Condition``, held by the caller of ``wait``
   * - Interrupting a blocked thread
     - Unsupported, as only the main thread can be interrupted, by ``KeyboardInterrupt``
   * - Flags enumeration
     - Unsupported. A flags enumeration is a ``DocEnum``, which raises ``ValueError`` for a combination
