C# Client
=========

This page covers using the kRPC C# client with the ``KSA`` service.

Installing
----------

Add the `KRPC.Client <https://www.nuget.org/packages/KRPC.Client>`_ @KRPC_VERSION@ NuGet
package to the project, and ``csharp/KSA.cs`` from the ``stubs`` folder of the release
archive.

Using the KSA Service
---------------------

The service is ``connection.KSA ()``:

.. code-block:: csharp

   using KRPC.Client;
   using KRPC.Client.Services.KSA;

   var connection = new Connection ("Example");
   var vehicle = connection.KSA ().ActiveVehicle;
   Console.WriteLine (vehicle.Name);

A member that can return nothing, such as ``Part.Engine``, returns ``null``.

The `kRPC C# client guide <https://krpc.github.io/krpc/@KRPC_VERSION@/csharp/client.html>`_ covers connecting, streams, events
and the client's other features.
