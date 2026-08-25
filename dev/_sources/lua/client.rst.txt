.. default-domain:: lua
.. highlight:: lua

Lua Client
==========

This client provides functionality to interact with a kRPC server from programs
written in Lua. It can be `installed using LuaRocks
<https://luarocks.org/modules/djungelorm/krpc>`_ or
`downloaded from GitHub <https://github.com/krpc/krpc/releases>`_.

Installing
----------

The Lua client and all of its dependencies can be installed using luarocks with
a single command:

.. code-block:: bash

   luarocks install krpc

Getting Started
---------------

Once it's installed, simply ``require 'krpc'`` and you are good to go!

Connecting to the Server
^^^^^^^^^^^^^^^^^^^^^^^^

To connect to a server, use the :func:`krpc.connect` function. This returns a
connection object through which you can interact with the server. For example to
connect to a server running on the local machine:

.. literalinclude:: /scripts/client/lua/Basic.lua

This function also accepts arguments that specify what address and port numbers
to connect to. For example:

.. literalinclude:: /scripts/client/lua/Connecting.lua

Interacting with the Server
---------------------------

Interaction with the server is performed via the client object (of type
:class:`krpc.Client`) returned when connecting to the server using
:func:`krpc.connect`.

Upon connecting, the client interrogates the server to find out what
functionality it provides and dynamically adds all of the classes, methods,
properties to the client object.

For example, all of the functionality provided by the SpaceCenter service is
accessible via ``conn.space_center`` and the functionality provided by the
InfernalRobotics service is accessible via ``conn.infernal_robotics``.

Calling methods, getting or setting properties, etc. are mapped to remote
procedure calls and passed to the server by the lua client.

Many procedures return an object identifying something in the game, such as a vessel or
a part. See :doc:`Object Lifetime </tutorials/object-lifetime>` for how such an object
behaves when a game is loaded or what it identifies is destroyed.

Streams and Events
------------------

These features are not supported by the Lua client.

Reference
---------

.. module:: krpc

.. function:: connect([name=''], [address='127.0.0.1'], [rpc_port=50000])

   This function creates a connection to a kRPC server. It returns a
   :class:`krpc.Client` object, through which the server can be communicated
   with.

   :param string name: A descriptive name for the connection. This is passed to
                       the server and appears, for example, in the client
                       connection dialog on the in-game server window.
   :param string address: The address of the server to connect to. Can either be
                          a hostname or an IP address in dotted decimal
                          notation. Defaults to '127.0.0.1'.
   :param number rpc_port: The port number of the RPC Server. Defaults to 50000.

.. function:: krpc.connect_local([name], [rpc_path])

   This function connects to a kRPC server running on the same machine, over a
   unix domain socket rather than TCP/IP. It returns a :class:`Client` object,
   just as :func:`krpc.connect` does, and the connection behaves identically
   thereafter.

   Unix domain sockets are available on Linux, macOS, and Windows 10 1803 and
   Windows Server 2019 or later. This needs luasocket's ``socket.unix`` module,
   which luasocket builds itself everywhere but Windows; there it comes from the
   ``luasocket-unix-windows`` rock, which ``luarocks install krpc`` pulls in. A
   luasocket built by hand needs that module built with it.

   :param string name: A descriptive name for the connection. This is passed to
                       the server and appears, for example, in the client
                       connection dialog on the in-game server window.
   :param string rpc_path: The path of the socket the RPC Server is listening
                           on. This should match the RPC socket path shown in
                           the in-game server window. Defaults to the path the
                           server uses unless it was configured with another.

.. class:: Client

   This class provides the interface for communicating with the server. It is
   dynamically populated with all the functionality provided by the
   server. Instances of this class should be obtained by calling
   :func:`krpc.connect`.

   .. method:: close()

      Closes the connection to the server.

   .. attribute:: krpc

      The built-in KRPC class, providing basic interactions with the server.

      :rtype: :class:`krpc.KRPC`

.. class:: KRPC

      This class provides access to the basic server functionality provided by
      the ``KRPC`` service. An instance can be obtained by calling
      :attr:`krpc.Client.krpc`. Most of this functionality is used internally by
      the lua client and therefore does not need to be used directly from
      application code. The only exception that may be useful is:

      .. method:: get_status()

         Gets a status message from the server containing information including
         the server's version string and performance statistics.

         For example, the following prints out the version string for the
         server:

         .. literalinclude:: /scripts/client/lua/ServerVersion.lua

         Or to get the rate at which the server is sending and receiving data
         over the network:

         .. literalinclude:: /scripts/client/lua/ServerStats.lua

Numeric Limits
--------------

The ``krpc.limits`` module names the extremes of the numeric types kRPC carries over the wire.
Lua gained ``math.maxinteger`` and ``math.mininteger`` in 5.3, and this client targets 5.1 and
5.2. A service can declare one of these limits as a parameter's default value, and it is then
documented as the constant here.

The minimum of an unsigned type is ``0``, so it has no constant.

.. note::

   Every Lua 5.1 and 5.2 number is a double, which holds each of these exactly except the two
   64-bit integer maxima: :data:`krpc.limits.SINT64_MAX` and :data:`krpc.limits.UINT64_MAX`
   round up to :math:`2^{63}` and :math:`2^{64}`. This is a property of the number type rather
   than of the constants, and applies equally to a 64-bit integer arriving from the server.

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
