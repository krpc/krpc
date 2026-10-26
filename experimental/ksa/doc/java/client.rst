Java Client
===========

This page covers using the kRPC Java client with the ``KSA`` service.

Installing
----------

Put the kRPC Java client jar @KRPC_VERSION@, and its protobuf and javatuples dependencies, on
the classpath. Add ``java/krpc/client/services/KSA.java`` from the ``stubs`` folder of
the release archive to the sources.

Using the KSA Service
---------------------

The service is ``KSA.newInstance(connection)``:

.. code-block:: java

   import krpc.client.Connection;
   import krpc.client.services.KSA;

   try (Connection connection = Connection.newInstance("Example")) {
     KSA ksa = KSA.newInstance(connection);
     System.out.println(ksa.getActiveVehicle().getName());
   }

A member that can return nothing, such as ``Part.Engine``, returns ``null``.

The `kRPC Java client guide <https://krpc.github.io/krpc/@KRPC_VERSION@/java/client.html>`_ covers connecting, streams, events
and the client's other features.
