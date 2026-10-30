C-nano
======

This page covers using the kRPC C-nano client with the ``KSA`` service. The
`kRPC C-nano client guide <https://krpc.github.io/krpc/@KRPC_VERSION@/cnano/client.html>`_ covers the client in full.

.. note::

   kRPC for KSA is experimental, and the client package does not yet include the ``KSA``
   service stubs. You add them from the release archive, as below, until a later release makes
   this easier.

Installing
----------

The server listens over TCP/IP, so build the kRPC C-nano client @KRPC_VERSION@ with its
``tcp`` feature, for example from `vcpkg <https://vcpkg.io>`_:

.. code-block:: bash

   vcpkg install "krpc-cnano[tcp]"

Add ``cnano/include`` from the ``stubs`` folder of the release archive to the include
path.

The `kRPC C-nano client guide <https://krpc.github.io/krpc/@KRPC_VERSION@/cnano/client.html#installing>`_ covers other ways to
install the client.

Connecting to the Server
------------------------

Open the connection with the address and RPC port of the server. Include ``krpc_cnano.h``
before the service header:

.. code-block:: c

   #include <stdio.h>
   #include <stdlib.h>
   #include <krpc_cnano.h>
   #include <krpc_cnano/services/ksa.h>

   krpc_connection_t conn;
   krpc_connection_config_t config = {"127.0.0.1", 50000};
   krpc_open(&conn, &config);
   krpc_connect(conn, "Example");

   krpc_KSA_Vehicle_t vehicle;
   krpc_KSA_ActiveVehicle(conn, &vehicle);
   char * name = NULL;
   krpc_KSA_Vehicle_Name(conn, &name, vehicle);
   printf("%s\n", name);
   free(name);

The `kRPC C-nano client guide <https://krpc.github.io/krpc/@KRPC_VERSION@/cnano/client.html>`_ covers the build options and the
client's other features.

API Reference
-------------

.. toctree::
   :maxdepth: 1

   cnano/api/ksa
