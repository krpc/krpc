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

The collector frees these cycles once the threads end, except that ``types._wrapped_subclasses``
keeps a client with stubs. A callback holding its own stream or the client needs no special
handling.

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
  through any handle, a server ending or the close. ``Client.stream()`` is the ``with`` form of
  ``add_stream``, and removes the stream at the end of the block.

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

Pre-Generated and Dynamic Services
----------------------------------

.. list-table::
   :header-rows: 1

   * - 
     - Pre-generated
     - Dynamic
   * - Source
     - ``krpc/services/``, from ``clientgen`` and ``python.tmpl`` in ``krpctools``
     - ``create_service``, from ``GetServices`` at connect
   * - Used for
     - Every service with a stub, by default
     - Other services, or all with ``use_pregenerated_stubs=False``
   * - Class
     - One per process, shared by every client
     - One per client, in its ``Types``
   * - Static method
     - ``WrappedClass``, a per-client subclass
     - Closure over the client

* The stubs are optional. Without them, every service the server has is dynamic.
* ``clientgen`` uses the Python client without pre-generated stubs.
* A connect registers the types of every service, then builds the services. The types come first as
  a procedure can use a type from any service. A parameter's default value can also be an
  enumeration value, which cannot be decoded until that enumeration is registered.
* A structure decodes using the field types the server declares, with or without stubs. A stub only
  sets the Python class of the decoded value.
* Fields are only ever appended to a structure, so a server declaring fewer fields than the stub is
  older. The client then warns, and decodes the structure as a plain named tuple of the fields that
  server declares.
* A class member only the server declares goes on a per-client subclass of the stub class, as every
  client in the process shares the stub class.

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
