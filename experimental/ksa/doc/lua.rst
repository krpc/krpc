Lua
===

This page covers using the kRPC Lua client with the ``KSA`` service. The
`kRPC Lua client guide <https://krpc.github.io/krpc/@KRPC_VERSION@/lua/client.html>`_ covers the client in full.

.. note::

   kRPC for KSA is experimental. A later release will make the client easier to use with
   the ``KSA`` service.

Installing
----------

Install the kRPC Lua client @KRPC_VERSION@ from `LuaRocks
<https://luarocks.org/modules/djungelorm/krpc>`_:

.. code-block:: bash

   luarocks install krpc @KRPC_VERSION@

The `kRPC Lua client guide <https://krpc.github.io/krpc/@KRPC_VERSION@/lua/client.html#installing>`_ covers other ways to
install the client.

Connecting to the Server
------------------------

The client builds the ``KSA`` service from the definitions the server sends when it connects,
so the service is ``conn.ksa``:

.. code-block:: lua

   local krpc = require 'krpc'
   local conn = krpc.connect('Example')
   print(conn.ksa.active_vehicle.name)

The `kRPC Lua client guide <https://krpc.github.io/krpc/@KRPC_VERSION@/lua/client.html>`_ covers connecting, streams and the
client's other features.

API Reference
-------------

.. toctree::
   :maxdepth: 1

   lua/api/ksa
