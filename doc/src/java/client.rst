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

.. code-block:: java

   import java.io.IOException;
   import krpc.client.Connection;
   import krpc.client.RPCException;
   import krpc.client.services.SpaceCenter;
   import krpc.client.services.SpaceCenter.Vessel;

   public class Program
   {
       public static void main (String[] args) throws IOException, RPCException
       {
           Connection connection = Connection.newInstance("Vessel Name");
           SpaceCenter sc = SpaceCenter.newInstance(connection);
           Vessel vessel = sc.getActiveVessel();
           System.out.println(vessel.getName());
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
overheads, as the ``vessel.position()`` function is called repeatedly.

.. code-block:: java

   Vessel vessel = spaceCenter.getActiveVessel();
   ReferenceFrame refframe = vessel.getOrbit().getBody().getReferenceFrame();
   while (true)
       System.out.println(vessel.position(refframe));

The following code achieves the same thing, but is far more efficient. It makes
a single call to :meth:`Connection.addStream` to create the stream, which avoids
the communication overhead in the previous example.

.. code-block:: java

   Vessel vessel = spaceCenter.getActiveVessel();
   ReferenceFrame refframe = vessel.getOrbit().getBody().getReferenceFrame();
   Stream<Triplet<Double,Double,Double>> vessel_stream = connection.addStream(vessel, "position", refframe);
   while (true)
       System.out.println(vessel_stream.get());;

Streams are created by calling :meth:`Connection.addStream` and passing it
information about which method to stream. The example above passes a remote
object, the name of the method to call, followed by the arguments to pass to the
method (if any). The most recent value for the stream can be obtained by calling
:meth:`Stream.get`.

Streams can also be added for static methods as follows:

.. code-block:: java

   Stream<Double> time_stream = connection.addStream(SpaceCenter.class, "getUt");

A stream can be removed by calling :meth:`Stream.remove()`.

Reference
---------

.. package:: krpc.client

.. type:: class Connection

   This class provides the interface for communicating with the server.

   .. method:: static Connection newInstance()
   .. method:: static Connection newInstance(String name)
   .. method:: static Connection newInstance(String name, String address)
   .. method:: static Connection newInstance(String name, String address, int rpcPort, int streamPort)
   .. method:: static Connection newInstance(String name, InetAddress address)
   .. method:: static Connection newInstance(String name, InetAddress address, int rpcPort, int streamPort)

      Create a connection to the server, using the given connection details.

      :param String name: A descriptive name for the connection. This is passed to
                          the server and appears, for example, in the client
                          connection dialog on the in-game server window.
      :param String address: The address of the server to connect to. Can either be
                             a hostname, an IP address as a string or an
                             InetAddress object. Defaults to '127.0.0.1'.
      :param int rpc_port: The port number of the RPC Server. Defaults to 50000.
      :param int stream_port: The port number of the Stream Server. Defaults
                              to 50001.

  .. method:: void close()

      Close the connection.

  .. method:: Stream<T> addStream(Class<?> clazz, String method, Object... args)

     Create a stream for a static method call to the given class.

  .. method:: Stream<T> addStream(RemoteObject instance, String method, Object... args)

     Create a stream for a method call to the given remote object.

.. type:: class Stream<T>

   A stream object.

   .. method:: T get()

      Get the most recent value for the stream.

   .. method:: void remove()

      Remove the stream from the server.

.. type:: abstract class RemoteObject

   The abstract base class for all remote objects.
