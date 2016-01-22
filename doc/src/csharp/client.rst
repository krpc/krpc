.. default-domain:: cs
.. highlight:: csharp

C# Client
=========

The ``KRPC.Client.dll`` assembly provides functionality to interact with a kRPC
server from C#.

Connecting to the Server
------------------------

To connect to a server, create a :class:`KRPC.Client.Connection` object. For
example to connect to a server running on the local machine:

.. code-block:: csharp

   using KRPC.Client.Services.KRPC;

   class Program
   {
       public static void Main ()
       {
           var connection = new KRPC.Client.Connection (name : "Example");
           System.Console.WriteLine (connection.KRPC ().GetStatus ().Version);
       }
   }

The class constructor also accepts arguments that specify what address and port
numbers to connect to. For example:

.. code-block:: csharp

   using KRPC.Client.Services.KRPC;

   class Program
   {
       public static void Main ()
       {
           var connection = new KRPC.Client.Connection (name : "Example", address: "my.domain.name", rpcPort: 1000, streamPort: 1001);
           System.Console.WriteLine (connection.KRPC ().GetStatus ().Version);
       }
   }

Interacting with the Server
---------------------------

Interaction with the server is performed via a connection object. Functionality
for services are defined in the namespaces ``KRPC.Client.Services.*``.

Before a service can be used it must first be instantiated. The following
example connects to the server, instantiates the SpaceCenter service, and
outputs the name of the active vessel:

.. code-block:: csharp

   using KRPC.Client.Services.SpaceCenter;

   class Program
   {
       public static void Main ()
       {
           var connection = new KRPC.Client.Connection (name : "Vessel Name");
           var sc = connection.SpaceCenter ();
           var vessel = sc.ActiveVessel;
           System.Console.WriteLine (vessel.Name);
       }
   }

Streaming Data from the Server
------------------------------

Streams are not yet supported by the C# client.
