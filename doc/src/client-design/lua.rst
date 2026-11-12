Lua Client Design
=================

The internal design of the Lua client. This page builds on the :doc:`shared client design
</client-design>` and the :doc:`client specification </client-spec>`. Usage is in
the :doc:`Lua guide </lua>`. Paths are relative to ``client/lua/``.

Ownership
---------

``krpc.connect`` returns a ``Connection`` table. The garbage collector tracks every holder, and an
object holds its client through the methods of its class:

.. code-block:: lua

   local function active_vessel()
     local conn = krpc.connect()
     return conn.space_center.active_vessel
     -- Holds the connection after conn goes out of scope
   end

References
^^^^^^^^^^

The client holds its ``Transport``, ``Types`` and service instances. The ``Transport`` holds the
luasocket socket, and ``Types`` holds the class, enumeration and structure tables.

Each connection has its own ``Types``, and so its own class tables. ``==`` compares object ids
across connections. An object passed to another connection is rebuilt in that connection's class.

The objects the program keeps also hold the client. An ``Object`` holds its class tables, whose
method closures hold the ``Connection``. A service instance holds its service class, which also
holds the ``Connection``.

The cycles are collected together. The socket's finalizer is the only finalizer among these objects.
Enumeration values hold no client. A structure holds one only through a field holding an object.

Lifetime
^^^^^^^^

* The connection closes on ``close()``, on a transport or protocol failure, or when the socket is
  collected.
* ``close()`` closes the sending side of the socket, then drains it for up to a second.
* After a close or a failure, ``Connection._error`` holds the ``ConnectionError``, and a call
  through any holder raises it. A second ``close()`` does nothing.
* ``Connection`` has no ``__gc`` or ``__close``. luasocket's finalizer closes a collected socket,
  including at the end of the program, and sends no ``disconnect``. An orderly close needs
  ``close()``.

Single Thread
-------------

Calls follow the :doc:`single-threaded design </client-design>`, on a blocking socket, so the client
has no polling and no ``socket.select``.

* Only the handshake and the drain of a close have a socket timeout, as a luasocket timeout adds a
  cost to every call. A stalled message blocks a call, with no way to interrupt it.
* No client code yields. A call blocks every coroutine in the Lua state, and two calls on one state
  do not interleave.

Dynamic Stubs
-------------

The client has no generated code. ``Connection:_init`` calls ``KRPC.GetServices``, registers the
types of every service, then builds each service, as a procedure can name a type from any service.

* ``_construct_func`` compiles each procedure with ``load``, into a function with fixed parameters.
  Arguments are read by position, so a ``nil`` leaves a hole and the later ones are still sent.
* Service and class tables derive from Penlight's ``class.properties``. A property is a ``get_x``
  and a ``set_x`` on the class table, reached through ``__index`` and ``__newindex``.
* A member naming a type this client does not know is skipped, with a warning on standard error, so
  a newer server still connects.
* A deprecated type or enumeration value is left out of its class table, and ``__index`` warns and
  returns it. The other members keep a plain table lookup. A structure field is an entry on the
  value, read with no hook, so it gives no warning.
* ``Types`` records the deprecated members it has warned of, so a loop does not repeat a warning.

Null Values
-----------

.. list-table::
   :header-rows: 1

   * - Position
     - Null
   * - Result
     - ``nil``, so a caller needs no sentinel check
   * - ``nil`` at a parameter with a default
     - Left out, so the server applies the default
   * - ``nil`` at a parameter with no default
     - Null if the parameter is nullable, otherwise an error before sending
   * - Explicit null at a parameter with a default
     - ``Types.none``
   * - Inside a list, tuple, dictionary or structure
     - ``Types.none``, as a table cannot hold ``nil``
   * - ``nil`` at an argument of ``run``
     - Null, as a function's parameter has no default

Server Side Functions
---------------------

The client has no function compiler, so a program builds a function with the expression API. There
are no function streams or events, as the client has no streams.

* **The stub names the call.** A Lua function has no name or parameter list to read, so
  ``_construct_func`` records each stub's procedure in ``service.procedures``. This covers a
  property's ``get_x`` and ``set_x``. ``get_call`` checks that one of a service's properties is
  given the service, as a value in that position would otherwise be taken for it.
* **The procedure table holds no types.** The keys of ``service.procedures`` are weak, and Lua 5.1
  and LuaJIT have no ephemeron tables, so a value reaching a class table would keep the client
  alive. The types are on the client, by qualified name, so a stub from another connection encodes
  with this connection's types.
* **The function supplies its types.** ``run`` reads the types of the parameters and the value from
  the ``KRPC.Expression``, through the connection running it, once per function. It caches them by
  object id. A null value is ``nil`` at any type.

Lua Versions
------------

The client runs on Lua 5.1 to 5.4 and LuaJIT. CI builds and tests on 5.4 only.

* Version differences live in ``krpc/compat.lua`` and Penlight's ``pl.compat``. luacheck's ``min``
  std flags a global that any supported version lacks.
* ``compat.native_integers`` picks the number model once, by whether ``math.type`` exists. The
  doubles of 5.1, 5.2 and LuaJIT are described in the guide's Numeric Limits.

lua-protobuf
------------

* lua-protobuf keeps types and options in one state per Lua state. The client loads its schema into
  a private state, and swaps it in with ``protobuf.call``, so a script's schemas and options stay
  separate. A response swaps once, for the message and its value together.
* ``krpc/encoder.lua`` writes requests and collections by hand with ``pb.buffer``, and
  ``krpc/decoder.lua`` reads collections with ``pb.slice``, so a call builds no message tables.

Transports
----------

``Transport`` holds the socket and the message framing. ``TCPIPTransport`` and ``LocalTransport``
derive from it and differ only in ``_open``.

* ``socket.unix`` is required when the socket opens, so a luasocket without it still connects over
  TCP/IP. Stock luasocket lacks it on Windows, pending
  `luasocket#469 <https://github.com/lunarmodules/luasocket/pull/469>`_.
  The test build pins a luasocket that has it, so Windows CI covers ``connect_local``.
* The default socket path takes the user name from ``USER`` or ``LOGNAME``, or on Windows from
  ``USERNAME``, as Lua can only read the environment. With no user name, the connect raises an
  error.

Mapping the Spec to Lua
-----------------------

.. list-table::
   :header-rows: 1

   * - Spec
     - Lua
   * - Remote error
     - An object of a ``pl.class`` class from ``krpc/error.lua``: ``RemoteError``, and
       ``ConnectionError``, ``ServerError`` and ``EncodingError`` deriving it. Raised with
       ``error(err, 0)``, so the message has no source position. Lua 5.1 prints an uncaught error
       object as "(error object is not a string)"
   * - Server exception with a mapped type
     - ``ServerError``, with its ``service`` and ``name``, as Lua has no native error types
   * - Argument error
     - A string
   * - Named arguments
     - Unsupported. ``nil`` skips a position
   * - List, tuple, set, dictionary
     - ``pl.List``, ``pl.List``, ``pl.Set``, ``pl.Map``. A table key compares by identity
   * - Protobuf message result
     - A plain table
   * - Getting a call as a value
     - ``Connection:get_call``, given a stub
   * - Value type of a server side function
     - The function's own type, read from the server
   * - Enumeration value
     - An instance of the connection's class, with the number in ``.value``
   * - Flags enumeration value
     - An instance of a ``Types.RemoteFlags`` class, combined with its methods, as Lua 5.1 has no bitwise operators to overload
