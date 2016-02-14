.. default-domain:: java

Java Client
===========

The ``krpc.client`` package provides functionality to interact with a kRPC server from
Java.

Connecting to the Server
------------------------

To connect to a server, use the :meth:`Connection.newInstance()` function. This returns a
connection object through which you can interact with the server. For example to
connect to a server running on the local machine:

.. code-block:: java

   Connection conn = Connection.newInstance("Example");
   System.out.println(KRPC.newInstance(conn).getStatus().getVersion());

This function also accepts arguments that specify what address and port numbers
to connect to. For example:

.. code-block:: java

   Connection conn = Connection.newInstance("Remote example", "my.domain.name", 1000, 1001);
   System.out.println(KRPC.newInstance(conn).getStatus().getVersion());

Interacting with the Server
---------------------------

Interaction with the server is performed via a connection object. Functionality
for services are defined in the package ``krpc.client.services.*``.

Before a service can be used it must first be instantiated. The following
example connects to the server, instantiates the SpaceCenter service, and
outputs the name of the active vessel:

.. code-block:: csharp

   import krpc.client.services.SpaceCenter;
   import krpc.client.services.SpaceCenter.Vessel;

   public class Program
   {
       public static void main (String[] args)
       {
           Connection connection = Connection.newInstance("Vessel Name");
           SpaceCenter sc = SpaceCenter.newInstance(connection);
           Vessel vessel = sc.getActiveVessel();
           System.out.println(vessel.getName());
       }
   }

Streaming Data from the Server
------------------------------

Not yet supported.
