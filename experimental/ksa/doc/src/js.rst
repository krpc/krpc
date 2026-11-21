JavaScript
==========

This page covers using the kRPC JavaScript client with the ``KSA`` service. The
`kRPC JavaScript client guide <https://krpc.github.io/krpc/@KRPC_VERSION@/js.html>`_ covers the client in full.

.. note::

   kRPC for KSA is experimental, and the client package does not yet include the ``KSA``
   service stubs. You add them from the release archive, as below, until a later release makes
   this easier.

Installing
----------

Install the client from npm:

.. code-block:: bash

   npm install @krpc-org/krpc@@KRPC_VERSION@

Then copy ``js/KSA.js`` and ``js/KSA.d.ts`` from the ``stubs`` folder of the release archive
beside your script. The client builds the ``KSA`` service when it connects, and the stubs give
it the service's types and values.

Connecting to the Server
------------------------

The client connects over WebSockets, to the server's WebSockets server on port 50001.

Import ``KSA.js`` before you connect, and the service is ``conn.ksa``:

.. code-block:: javascript

   import { connect } from "@krpc-org/krpc";
   import "./KSA.js";

   const conn = await connect({ name: "Example" });
   const vehicle = await conn.ksa.getActiveVehicle();
   console.log(await vehicle.getName());

The `kRPC JavaScript client guide <https://krpc.github.io/krpc/@KRPC_VERSION@/js.html>`_ covers connecting, streams, events
and the client's other features.

API Reference
-------------

.. toctree::
   :maxdepth: 1

   js/api/ksa
