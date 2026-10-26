C++ Client
==========

This page covers using the kRPC C++ client with the ``KSA`` service.

Installing
----------

Install the kRPC C++ client @KRPC_VERSION@, for example from `vcpkg <https://vcpkg.io>`_:

.. code-block:: bash

   vcpkg install krpc

Add ``cpp/include`` from the ``stubs`` folder of the release archive to the include
path.

Using the KSA Service
---------------------

The service is ``krpc::services::KSA``:

.. code-block:: cpp

   #include <iostream>
   #include <krpc.hpp>
   #include <krpc/services/ksa.hpp>

   krpc::Client conn = krpc::connect("Example");
   krpc::services::KSA ksa(&conn);
   std::cout << ksa.active_vehicle()->name() << std::endl;

A member that can return nothing, such as ``Part.Engine``, returns a ``std::optional``:

.. code-block:: cpp

   for (auto part : ksa.active_vehicle()->parts())
     if (auto engine = part.engine())
       std::cout << engine->thrust() << std::endl;

The `kRPC C++ client guide <https://krpc.github.io/krpc/@KRPC_VERSION@/cpp/client.html>`_ covers connecting, streams, events
and the client's other features.
