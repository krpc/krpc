Clients
=======

kRPC for KSA works with the kRPC @KRPC_VERSION@ client libraries. Python and Lua build the
``KSA`` service when they connect. The C#, C++, C-nano and Java clients compile against
the stubs in ``krpc-ksa-stubs-@VERSION@.zip``.

.. tabs::

   .. group-tab:: Python

      The service is ``conn.ksa``:

      .. code-block:: python

         import krpc
         conn = krpc.connect()
         print(conn.ksa.active_vehicle.name)

      The client has no type stubs for the service, so a type checker reports
      ``conn.ksa`` as an unknown attribute.

   .. group-tab:: C#

      Add ``csharp/KSA.cs`` to a project that references the ``KRPC.Client`` package:

      .. code-block:: csharp

         using KRPC.Client.Services.KSA;

         var ksa = connection.KSA ();
         Console.WriteLine (ksa.ActiveVehicle.Name);

   .. group-tab:: C++

      Add ``cpp/include`` to the include path, beside the kRPC C++ client:

      .. code-block:: cpp

         #include <krpc.hpp>
         #include <krpc/services/ksa.hpp>

         krpc::services::KSA ksa(&conn);
         std::cout << ksa.active_vehicle()->name() << std::endl;

   .. group-tab:: C-nano

      Add ``cnano/include`` to the include path, beside the kRPC C-nano client. Include
      ``krpc_cnano.h`` first:

      .. code-block:: c

         #include <krpc_cnano.h>
         #include <krpc_cnano/services/ksa.h>

         krpc_KSA_Vehicle_t vehicle;
         krpc_KSA_ActiveVehicle(conn, &vehicle);

   .. group-tab:: Java

      Compile ``java/krpc/client/services/KSA.java`` with the kRPC Java client jar on the
      classpath:

      .. code-block:: java

         import krpc.client.services.KSA;

         KSA ksa = KSA.newInstance(connection);
         System.out.println(ksa.getActiveVehicle().getName());

   .. group-tab:: Lua

      The service is ``conn.ksa``:

      .. code-block:: lua

         local krpc = require 'krpc'
         local conn = krpc.connect()
         print(conn.ksa.active_vehicle.name)

The stubs archive also holds ``KRPC.KSA.json``, the service definitions. ``krpc-clientgen``
from the ``krpctools`` package generates stubs from it for a client version of your choice.
