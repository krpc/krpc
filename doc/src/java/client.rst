.. default-domain:: java
.. highlight:: java

Java Client
===========

This client provides functionality to interact with a kRPC server from programs
written in Java. A jar containing the ``krpc.client`` package can be
:github-download-jar:`downloaded from GitHub <krpc-java>`.
It requires Java version 1.7.

Using the Library
----------------------

The kRPC client library depends on the `protobuf
<https://github.com/google/protobuf/tree/master/java>`_ and `javatuples
<http://www.javatuples.org>`_ libraries. A prebuilt jar for protobuf is
available via `Maven
<http://search.maven.org/#search|ga|1|g%3A%22com.google.protobuf%22%20a%3A%22protobuf-java%22>`_. Note
that you need protobuf version 3. Version 2 is not compatible with kRPC.

The following example program connects to the server, queries it for its version
and prints it out:

.. literalinclude:: /scripts/Basic.java

To compile this program using javac on the command line, save the source as
``Example.java`` and run the following:

.. code-block:: bash

   javac -cp krpc-java-0.4.0.jar:protobuf-java-3.1.0.jar:javatuples-1.2.jar Example.java

You may need to change the paths to the JAR files.

Connecting to the Server
------------------------

To connect to a server, use the :meth:`Connection.newInstance()` function. This
returns a connection object through which you can interact with the server. When
called without any arguments, it will connect to the local machine on the
default port numbers. You can specify different connection settings, including a
descriptive name for the client, as follows:

.. literalinclude:: /scripts/Connecting.java

Interacting with the Server
---------------------------

Interaction with the server is performed via a connection object. Functionality
for services are defined in the packages ``krpc.client.services.*``. Before a
service can be used it must first be instantiated. The following example
connects to the server, instantiates the SpaceCenter service, and outputs the
name of the active vessel:

.. literalinclude:: /scripts/Interacting.java

.. _java-client-streams:

Streaming Data from the Server
------------------------------

A stream repeatedly executes a function on the server, with a fixed set of
argument values. It provides a more efficient way of repeatedly getting the
result of a function, avoiding the network overhead of having to invoke it
directly.

For example, consider the following loop that continuously prints out the
position of the active vessel. This loop incurs significant communication
overheads, as the ``vessel.position()`` function is called repeatedly.

.. literalinclude:: /scripts/Streaming.java

The following code achieves the same thing, but is far more efficient. It calls
:meth:`Connection.addStream` once at the start of the program to create a
stream, and then repeatedly gets the position from the stream.

.. literalinclude:: /scripts/Streaming2.java

Streams are created by calling :meth:`Connection.addStream` and passing it
information about which method to stream. The example above passes a remote
object, the name of the method to call, followed by the arguments to pass to the
method (if any). The most recent value for the stream can be obtained by calling
:meth:`Stream.get`.

Streams can also be added for static methods as follows:

.. code-block:: java

   Stream<Double> time_stream = connection.addStream(SpaceCenter.class, "getUt");

A stream can be removed by calling :meth:`Stream.remove()`. All of a clients
streams are automatically stopped when it disconnects.

Client API Reference
--------------------

.. package:: krpc.client

.. type:: class Connection

   This class provides the interface for communicating with the server.

   .. method:: static Connection newInstance()
   .. method:: static Connection newInstance(String name)
   .. method:: static Connection newInstance(String name, String address)
   .. method:: static Connection newInstance(String name, String address, int rpcPort, int streamPort)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address)
   .. method:: static Connection newInstance(String name, java.net.InetAddress address, int rpcPort, int streamPort)

      Create a connection to the server, using the given connection details.

      :param String name: A descriptive name for the connection. This is passed to
                          the server and appears, for example, in the client
                          connection dialog on the in-game server window.
      :param String address: The address of the server to connect to. Can either be
                             a hostname, an IP address as a string or a
                             :ref:`java.net.InetAddress` object. Defaults to "127.0.0.1".
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
