.. default-domain:: csharp
.. highlight:: csharp

C# Client
=========

The ``KRPC.Client.dll`` library provides functionality to interact with a kRPC
server from C#.

Installing the Client
---------------------

``KRPC.Client.dll`` is `available on NuGet
<https://www.nuget.com/packages/KRPC.Client/>`_. You also need to get
``Google.Protobuf.dll``, which is `also available on NuGet
<https://www.nuget.org/packages/Google.Protobuf/>`_.

.. note::

   The copy of ``Google.Protobuf.dll`` in the GameData folder shipped with kRPC
   should be *avoided*. It is a modified version to work within KSP.
   `See here for more details. <https://github.com/djungelorm/protobuf/releases/tag/v3.0.0-beta-2-net35>`_

Connecting to the Server
------------------------

To connect to a server, create a :type:`KRPC.Client.Connection` object. For
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

A stream repeatedly executes a function on the server, with a fixed set of
argument values. It provides a more efficient way of repeatedly getting the
result of calling function on the server, without having to invoke it directly
-- which incurs communication overheads.

For example, consider the following loop that continuously prints out the
position of the active vessel. This loop incurs significant communication
overheads, as the :meth:`Vessel.Position` method is called repeatedly.

.. code-block:: csharp

   var vessel = connection.SpaceCenter().ActiveVessel;
   var refframe = vessel.Orbit.Body.ReferenceFrame;
   while (True)
       Console.Out.WriteLine(vessel.Position(refframe));

The following code achieves the same thing, but is far more efficient. It makes
a single call to :meth:`Connection.AddStream` to create the stream, which avoids
the communication overhead in the previous example.

.. code-block:: csharp

   var vessel = connection.SpaceCenter().ActiveVessel;
   var refframe = vessel.Orbit.Body.ReferenceFrame;
   var position = conn.AddStream(() => vessel.Position(refframe));
   while (True)
       Console.Out.WriteLine(position.Get());

Streams are created by calling :meth:`Connection.AddStream` and passing it a
lambda expression. It returns an instance of the :type:`KRPC.Client.Stream`
class from which the latest value can be obtained by calling
:meth:`KRPC.Client.Stream.Get`.

The lambda expression passed to :meth:`Connection.AddStream` must take zero
arguments and be either a method call expression or a parameter call expression.
