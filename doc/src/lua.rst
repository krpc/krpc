.. default-domain:: lua
.. highlight:: lua

Lua
===

This guide describes how to install the Lua client, and the features it provides. The
:ref:`lua-client-reference` describes each function and class.

Installing the Library
----------------------

The library can be found on `LuaRocks <https://luarocks.org/modules/djungelorm/krpc>`_ or
`downloaded from GitHub <https://github.com/krpc/krpc/releases>`_. It runs on Lua 5.1 to 5.4 and
LuaJIT.

To install it and its dependencies using luarocks:

.. code-block:: bash

   luarocks install krpc

Connecting to the Server
------------------------

The game must be running with the server started. :ref:`getting-started-server` in the
getting started guide shows how to install and start it.

The client connects over TCP/IP or over a local socket. Each server in the server window uses one
protocol, and accepts connections over that protocol only. Pick the protocol when you add the
server, and connect with the matching function.

.. note::

   A server with "Auto-accept new clients" turned off answers only once the player approves the
   connection in game. The connection fails after 5 seconds by default. Pass ``false`` as the
   ``handshake_timeout`` to wait indefinitely for the player.

Over TCP/IP
^^^^^^^^^^^

Select "Protobuf over TCP/IP" as the server's protocol. :func:`~krpc.connect` connects to it, and
returns a :class:`~krpc.Connection`. You make every call through this object. The following connects
to a server with the default settings, address ``127.0.0.1`` and port 50000, and prints its version:

.. literalinclude:: /scripts/lua/Connecting1.lua

:func:`~krpc.connect` also takes a name for the connection, and the address and port of the server.
These match the address and port shown in the server window. The name appears in the server window
in the game:

.. literalinclude:: /scripts/lua/Connecting2.lua

TCP/IP works over a network, so your program can run on a different machine from the game.

Over a Local Socket
^^^^^^^^^^^^^^^^^^^

Select "Protobuf over local socket" as the server's protocol. :func:`~krpc.connect_local` connects
to it, and returns a :class:`~krpc.Connection` as :func:`~krpc.connect` does. The following connects
to a server with the default settings, at the default socket path for your platform, and prints its
version:

.. literalinclude:: /scripts/lua/ConnectingLocal.lua

:func:`~krpc.connect_local` also takes a ``path``, for a server with another socket path. The path
matches the one shown in the server window.

A local socket works only on the machine running the game. It has lower overhead than TCP/IP, so
it is faster.

.. note::

   A local socket needs luasocket's ``socket.unix`` module, which luasocket builds on Linux and
   macOS. On Windows, connect with :func:`~krpc.connect` instead.

Closing the Connection
^^^^^^^^^^^^^^^^^^^^^^

:meth:`~krpc.Connection.close` closes a connection:

.. literalinclude:: /scripts/lua/Connecting3.lua

A connection you do not close stays open until the garbage collector collects it, or your program
exits.

Calling Remote Procedures
-------------------------

The server provides *procedures* for your program to call, grouped into *services*.

The following gets the active vessel from :attr:`~SpaceCenter.active_vessel`, an object
of type :class:`~SpaceCenter.Vessel`. It sets the vessel's name, then prints its altitude:

.. literalinclude:: /scripts/lua/RemoteProcedures.lua

Each service is a field of the connection, such as ``conn.space_center`` for the :mod:`SpaceCenter`
service. A property is a field, as in ``vessel.name``. Call a method of an object with ``:``, as
in ``vessel:flight()``, and a procedure of a service with ``.``, as in
``conn.krpc.get_status()``.

Handling Errors
---------------

A procedure that fails on the server raises a Lua error in your program. The following catches the
error from loading a save that does not exist, using ``pcall``:

.. literalinclude:: /scripts/lua/Errors.lua

The error is an object, which ``tostring`` and ``..`` turn into its message. Its class says what
failed, and ``krpc.RemoteError:class_of(err)`` matches every error of the client's own:

* A procedure that fails on the server raises a :class:`krpc.ServerError`. Its ``service`` and
  ``name`` fields name the server's exception, such as ``KRPC`` and ``ArgumentException``.
* A failed connection attempt raises a :class:`krpc.ConnectionError`. When the server refuses the
  connection, its ``status`` field names the status it gave, such as ``DENIED``. When the server
  ends the connection, its ``reason`` field names the reason, such as ``SERVER_STOPPED``.
* A call made once the connection has closed or failed raises a :class:`krpc.ConnectionError`.
* A value that fails to decode raises a :class:`krpc.EncodingError`.
* An argument the client rejects raises a string.

.. _lua-client-values:

Values
------

Procedures take and return values of these types:

.. list-table::
   :header-rows: 1

   * - Type
     - Lua type
   * - Integer, floating point number, boolean
     - ``number``, ``boolean``
   * - String, bytes
     - ``string``
   * - Tuple, list
     - Penlight's ``List``
   * - Set, dictionary
     - Penlight's ``Set`` and ``Map``
   * - Class
     - A remote object, described below
   * - Enumeration
     - A value of the enumeration, such as ``conn.space_center.MapFilterType.debris``
   * - Structure
     - A value with a field for each field of the structure
   * - Null
     - ``nil``, or ``Types.none`` as described below

.. _lua-client-flags:

Flags Enumerations
^^^^^^^^^^^^^^^^^^

A flags enumeration, such as :class:`~SpaceCenter.MapFilterType`, holds any combination of its
values. You combine values with ``union``, and test for one with ``contains``:

.. literalinclude:: /scripts/lua/Flags.lua

``intersection`` and ``difference`` give the other combinations of two values.

Null Values
^^^^^^^^^^^

A procedure that can return null returns ``nil``. The following prints the name of the target
vessel, if there is one, then clears the target:

.. literalinclude:: /scripts/lua/NullValues.lua

A ``nil`` argument leaves the argument out, and the parameter takes its default. The arguments
after it are still passed. The following launches a vessel with the default crew, and passes
``false`` for the parameter after it:

.. literalinclude:: /scripts/lua/OmittedArguments.lua

At a parameter with no default, a ``nil`` argument is null if the parameter can be null. Otherwise
it raises an error.

A Lua table cannot hold ``nil``, so a null in a list, tuple, dictionary or structure is
:data:`Types.none <krpc.Types.none>`:

.. code-block:: lua

   local Types = require 'krpc.types'
   local values = {1, Types.none, 3}
   print(values[2] == Types.none)

Pass :data:`Types.none <krpc.Types.none>` as an argument for a null at a parameter whose default
is not null.

Remote Objects
--------------

Many procedures return an object identifying something in the game, such as a vessel or a part.
The object holds no copy of the game's state. It is just a reference to it. Each call through it
reads the game as it is now:

.. literalinclude:: /scripts/lua/RemoteObjects.lua

Two objects for the same thing in the game compare equal with ``==``.

Once the thing an object identifies is destroyed, such as a part that explodes, each call through
it raises an error naming :class:`KRPC.ObjectDestroyedException`.

An object for a vessel or a part keeps working when you load a save, quickload or revert, and
then reads the loaded game. Other objects depend on what they identify. A maneuver node, for
example, is destroyed by a load.

A vessel far from the active vessel is *unloaded*, and the game keeps no parts for it. The vessel
itself reads normally, but a call on one of its parts raises an error naming
:class:`KRPC.ObjectNotLoadedException`. The part works again once the vessel comes back into range.

.. seealso:: :doc:`/user-guide/object-lifetime` describes how each kind of object behaves in more
             detail.

Calling Methods
---------------

You call a method on an object of its class, with a colon. A *static* method needs no object, and
you call it on the class itself with a dot. Reach the class through the connection's service:

.. literalinclude:: /scripts/lua/StaticMethods.lua

The call runs on the connection the class is reached through. Called through an object, a static
method runs on the object's connection.

Server Side Functions
---------------------

A server side function runs inside the game. You build it in your program with the expression
API, and the client sends it to the server, which runs it there. The following reads the vessel's
altitude and speed together:

.. literalinclude:: /scripts/lua/RunFunction.lua

:meth:`~krpc.Connection.get_call` returns the call a procedure makes, as a message for
:meth:`~KRPC.Expression.call` to embed in the function. A property is read through its getter,
such as ``flight.get_speed``, with the object as the first argument.
:ref:`server-side-functions-api` in the :ref:`server side functions guide
<server-side-functions>` describes how to build functions.

Running a Function
^^^^^^^^^^^^^^^^^^

:meth:`~krpc.Connection.run` runs a function once, and returns its value. The server runs the whole
function within one physics tick, so every value it reads comes from the same moment in the game.
In the example, the altitude and the speed are consistent with each other, which two separate calls
cannot guarantee.

A function takes arguments through its parameters. The following compiles a function ahead of its
first run, then passes it an argument:

.. literalinclude:: /scripts/lua/RunFunctionArguments.lua

:meth:`~krpc.Connection.compile` makes the first run as fast as later ones.

Streams and Events
------------------

The Lua client has no streams or events. Read a value by calling its procedure in a loop instead.

Deprecated Members
------------------

A member of a service can be deprecated, when a newer member replaces it. A deprecated member
still works, but may be removed in a future release, so move to its replacement.

The API reference marks it as deprecated, and names the replacement. Using it writes a warning
naming the replacement to standard error, once per member on a connection.

Service Stubs
-------------

The Lua client has no pre-generated service stubs. When you connect, it builds every service from
the service definitions the server sends. The services match the server you connect to, whatever
its version, including services added by other mods.

.. _lua-client-reference:

Client API Reference
--------------------

.. module:: krpc

.. function:: connect(name?: string, address?: string, port?: number, handshake_timeout?: number | false): krpc.Connection

   Connects to a kRPC server, and returns a :class:`~krpc.Connection`.

   :param name: A name for the connection, shown in the server window in the game. Defaults to
                ``''``.
   :param address: The address of the server, as a host name or an IP address. Defaults to
                   ``'127.0.0.1'``.
   :param port: The port of the server. Defaults to 50000.
   :param handshake_timeout: How many seconds to wait for the server to accept the connection.
                             Defaults to 5. ``false`` waits indefinitely, such as for a server
                             that asks the player to approve each connection.

   Raises an error if the connection fails or the server refuses it, and for a timeout that is not
   positive.

.. function:: connect_local(name?: string, path?: string, handshake_timeout?: number | false): krpc.Connection

   Connects to a kRPC server on the same machine over a local socket, and returns a
   :class:`~krpc.Connection`. The server must use the local socket protocol.

   This needs luasocket's ``socket.unix`` module, which luasocket builds on Linux and macOS.

   :param name: A name for the connection, shown in the server window in the game. Defaults to
                ``''``.
   :param path: The path of the server's socket, shown in the server window in the game.
                Defaults to the path the server uses unless it was configured with another.
   :param handshake_timeout: As for :func:`~krpc.connect`.

   Raises an error as :func:`~krpc.connect` does, if ``socket.unix`` is not available, and if no
   ``path`` is given and the default path cannot be found.

.. class:: RemoteError

   The base of every error the client raises of its own, a ``pl.class`` class.
   ``RemoteError:class_of(err)`` checks whether a value is one.

   .. attribute:: message

      What failed, followed by the message of the cause, if there is one.

   .. attribute:: cause

      The error that caused this one, or ``nil``.

.. class:: ConnectionError

   Raised when connecting fails, and by each call once the connection has closed or failed. A
   subclass of :class:`krpc.RemoteError`.

   .. attribute:: status

      The status the server refused the connection with, such as ``'DENIED'``, or ``nil``.

   .. attribute:: reason

      The reason the server gave for ending the connection, such as ``'SERVER_STOPPED'``, or
      ``nil``.

.. class:: ServerError

   Raised for an error from the server. A subclass of :class:`krpc.RemoteError`.

   .. attribute:: service

      The service of the server's exception, such as ``'KRPC'``, or ``nil``.

   .. attribute:: name

      The name of the server's exception, such as ``'ArgumentException'``, or ``nil``.

.. class:: EncodingError

   Raised for a value that fails to decode. A subclass of :class:`krpc.RemoteError`.

.. function:: get_object_id(obj): number

   The identifier of a remote object on the server, e.g. to pass to
   :meth:`~KRPC.Expression.constant_object`. Raises an error if the object is ``nil`` or
   :data:`Types.none <krpc.Types.none>`.

   :param obj: The remote object.

.. class:: Connection

   A connection to the server, with a field for each of the server's services. Obtain one by
   calling :func:`~krpc.connect` or :func:`~krpc.connect_local`.

   .. method:: close()

      Closes the connection. Closing a connection that is already closed does nothing.

   .. method:: get_call(procedure: function, ...): table

      The call a procedure of a service or a class makes with the given arguments, as a
      ``KRPC.ProcedureCall`` message for :meth:`~KRPC.Expression.call`. A property is read
      through its getter, ``get_<name>``, and set through its setter, ``set_<name>``, each with
      the object or the service as the first argument.

      Raises an error if a service's property is not given the service.

   .. method:: compile(func: KRPC.Expression): KRPC.Expression

      Compiles a server side function on the server, so that its first run is as fast as later
      ones, and returns the function.

   .. method:: run(func: KRPC.Expression, ...): any

      Runs a server side function within a single physics tick, and returns its value. The
      arguments are passed to the function's parameters, with ``nil`` or :data:`Types.none
      <krpc.Types.none>` for a null. Returns ``nil`` for a function with no value, and for a null
      value.

      Raises an error if the number of arguments differs from the number of parameters, or an
      argument does not match its parameter.

   .. attribute:: krpc: KRPC

      The :mod:`KRPC` service, providing the server's basic functionality. Each of the server's
      other services is a field too, such as ``space_center``.

.. data:: Types.none

   The value that stands for null in a list, tuple, dictionary or structure, where ``nil`` cannot
   go, and in an argument whose default is not null. Load it with
   ``local Types = require 'krpc.types'``, and test for it with ``==``.

Numeric Limits
^^^^^^^^^^^^^^

The ``krpc.limits`` module names the extremes of the numeric types kRPC carries over the wire.
A service can declare one of these limits as a parameter's default value, and it is then
documented as the constant here.

The minimum of an unsigned type is ``0``, so it has no constant.

From Lua 5.3 on, a 64-bit value is an exact integer. Lua has no unsigned integer type. A
``uint64`` above :math:`2^{63} - 1` wraps to a negative integer, so
:data:`~krpc.limits.UINT64_MAX` is ``-1``. ``math.ult`` compares two such values.

.. note::

   In Lua 5.1, 5.2 and LuaJIT every number is a double, which holds an integer exactly up to
   :math:`2^{53}`. :data:`~krpc.limits.SINT64_MAX` and :data:`~krpc.limits.UINT64_MAX` round up
   to :math:`2^{63}` and :math:`2^{64}`. A 64-bit value from the server arrives as the nearest
   double.

.. data:: limits.DOUBLE_MAX
          limits.DOUBLE_LOWEST

   The largest and most negative finite 64-bit float.

.. data:: limits.FLOAT_MAX
          limits.FLOAT_LOWEST

   The largest and most negative finite 32-bit float.

.. data:: limits.SINT32_MAX
          limits.SINT32_MIN

   The largest and most negative 32-bit signed integer.

.. data:: limits.SINT64_MAX
          limits.SINT64_MIN

   The largest and most negative 64-bit signed integer.

.. data:: limits.UINT32_MAX

   The largest 32-bit unsigned integer.

.. data:: limits.UINT64_MAX

   The largest 64-bit unsigned integer.

API Reference
-------------

.. toctree::
   :maxdepth: 1

   lua/api/krpc
   lua/api/stdlib
   lua/api/space-center
   lua/api/drawing
   lua/api/ui
   lua/api/infernal-robotics
   lua/api/kerbal-alarm-clock
   lua/api/remote-tech
   lua/api/lidar
   lua/api/docking-camera
   lua/api/trajectories
   lua/api/mechjeb
   lua/api/kerbal-engineer-redux
   lua/api/scansat
   lua/api/debug
