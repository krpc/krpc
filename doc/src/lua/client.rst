.. default-domain:: lua
.. highlight:: lua

Lua Client
==========

This client provides functionality to interact with a kRPC server from programs
written in Lua. It can be `installed using LuaRocks
<https://luarocks.org/modules/djungelorm/krpc>`_ or
:github-download-zip:`downloaded from GitHub <krpc-lua>`.

Installing the Library
----------------------

The Lua client and all of its dependencies can be installed using luarocks with
a single command:

.. code-block:: bash

   luarocks install krpc

Using the Library
-----------------

Once it's installed, simply ``require 'krpc'`` and you are good to go!

Connecting to the Server
------------------------

To connect to a server, use the :func:`krpc.connect` function. This returns a
connection object through which you can interact with the server. For example to
connect to a server running on the local machine:

.. literalinclude:: /scripts/Basic.lua

This function also accepts arguments that specify what address and port numbers
to connect to. For example:

.. literalinclude:: /scripts/Connecting.lua

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

Streaming Data from the Server
------------------------------

Streams are not yet supported by the Lua client.

Reference
---------

.. module:: krpc

.. function:: connect([name=nil], [address='127.0.0.1'], [rpc_port=50000], [stream_port=50001])

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
   :param number stream_port: The port number of the Stream Server. Defaults
                              to 50001.

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

         .. literalinclude:: /scripts/ServerVersion.lua

         Or to get the rate at which the server is sending and receiving data
         over the network:

         .. literalinclude:: /scripts/ServerStats.lua
