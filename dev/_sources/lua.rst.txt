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
returns a :class:`~krpc.Client`. You make every call through this object. The following connects to
a server with the default settings, address ``127.0.0.1`` and port 50000, and prints its version:

.. literalinclude:: /scripts/lua/Connecting1.lua

:func:`~krpc.connect` also takes a name for the connection, and the address and port of the server.
These match the address and port shown in the server window. The name appears in the server window
in the game:

.. literalinclude:: /scripts/lua/Connecting2.lua

TCP/IP works over a network, so your program can run on a different machine from the game.

Over a Local Socket
^^^^^^^^^^^^^^^^^^^

Select "Protobuf over local socket" as the server's protocol. :func:`~krpc.connect_local` connects
to it, and returns a :class:`~krpc.Client` as :func:`~krpc.connect` does. The following connects to
a server with the default settings, at the default socket path for your platform, and prints its
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

:meth:`~krpc.Client.close` closes a connection:

.. literalinclude:: /scripts/lua/Connecting3.lua

Calling Remote Procedures
-------------------------

The server provides *procedures* for your program to call, grouped into *services*.

The following gets the active vessel from :attr:`~SpaceCenter.SpaceCenter.active_vessel`, an object
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

The error is a string describing the failure:

* A procedure that fails on the server raises an error whose message starts with the server's
  exception, as in ``KRPC.ArgumentException: Failed to load missing``.
* A failed connection attempt raises an error. When the server refuses the connection, the message
  names the status it gave, such as ``DENIED``. When the server ends the connection, the message
  names the reason, such as ``SERVER_STOPPED``.
* A call made once the connection has closed or failed raises an error.

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

Streams and Events
------------------

The Lua client has no streams or events. Read a value by calling its procedure in a loop instead.

Deprecated Members
------------------

A member of a service can be deprecated, when a newer member replaces it. A deprecated member
still works, but may be removed in a future release, so move to its replacement.

The API reference marks it as deprecated, and names the replacement.

Service Stubs
-------------

The Lua client has no pre-generated service stubs. When you connect, it builds every service from
the service definitions the server sends. The services match the server you connect to, whatever
its version, including services added by other mods.

.. _lua-client-reference:

Client API Reference
--------------------

.. module:: krpc

.. function:: connect([name=''], [address='127.0.0.1'], [port=50000], [handshake_timeout=5])

   Connects to a kRPC server, and returns a :class:`~krpc.Client`.

   :param string name: A name for the connection, shown in the server window in the game.
   :param string address: The address of the server, as a host name or an IP address. Defaults to
                          ``'127.0.0.1'``.
   :param number port: The port of the server. Defaults to 50000.
   :param number handshake_timeout: How many seconds to wait for the server to accept the
                          connection. Defaults to 5. ``false`` waits indefinitely, such as for a
                          server that asks the player to approve each connection.

   Raises an error if the connection fails or the server refuses it, and for a timeout that is not
   positive.

.. function:: connect_local([name=''], [path], [handshake_timeout=5])

   Connects to a kRPC server on the same machine over a local socket, and returns a
   :class:`~krpc.Client`. The server must use the local socket protocol.

   This needs luasocket's ``socket.unix`` module, which luasocket builds on Linux and macOS.

   :param string name: A name for the connection, shown in the server window in the game.
   :param string path: The path of the server's socket, shown in the server window in the game.
                       Defaults to the path the server uses unless it was configured with another.
   :param number handshake_timeout: As for :func:`~krpc.connect`.

   Raises an error as :func:`~krpc.connect` does, if ``socket.unix`` is not available, and if no
   ``path`` is given and the default path cannot be found.

.. function:: get_object_id(obj)

   The identifier of a remote object on the server, e.g. to pass to
   :meth:`~KRPC.Expression.constant_object`. Raises an error if the object is ``nil`` or
   :data:`Types.none <krpc.Types.none>`.

   :param obj: The remote object.
   :rtype: number

.. class:: Client

   A connection to the server, with a field for each of the server's services. Obtain one by
   calling :func:`~krpc.connect` or :func:`~krpc.connect_local`.

   .. method:: close()

      Closes the connection. Closing a connection that is already closed does nothing.

   .. attribute:: krpc

      The :mod:`KRPC` service, providing the server's basic functionality. Each of the server's
      other services is a field too, such as ``space_center``.

.. data:: Types.none

   The value that stands for null, in an argument or a result. Load it with
   ``local Types = require 'krpc.types'``. A procedure whose result is null returns it, and ``==``
   tests for it.

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
   lua/api/debug
