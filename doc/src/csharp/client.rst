.. default-domain:: csharp
.. highlight:: csharp

C# Client
=========

This client provides functionality to interact with a kRPC server from programs
written in C#. The ``KRPC.Client.dll`` assembly can be `installed using NuGet
<http://www.nuget.com/packages/KRPC.Client>`_ or
:github-download-zip:`downloaded from GitHub <krpc-csharp>`.

Installing the Library
----------------------

Install the client `using NuGet <http://www.nuget.com/packages/KRPC.Client>`_
or download the assembly :github-download-zip:`from GitHub <krpc-csharp>` and
reference it in your project. You also need to `install Google.Protobuf using
NuGet <http://www.nuget.org/packages/Google.Protobuf>`_.

.. note::

   The copy of ``Google.Protobuf.dll`` in the GameData folder shipped with the
   kRPC server plugin should be *avoided*. It is a modified version to work
   within KSP. `See here for more
   details. <https://github.com/djungelorm/protobuf/releases/tag/v3.0.0-beta-2-net35>`_

Connecting to the Server
------------------------

To connect to a server, create a :type:`Connection` object. For example to
connect to a server running on the local machine:

.. literalinclude:: /scripts/Basic.cs

The class constructor also accepts arguments that specify what address and port
numbers to connect to. For example:

.. literalinclude:: /scripts/Connecting.cs

Interacting with the Server
---------------------------

kRPC groups remote procedures into services. The functionality for the services
are defined in namespace ``KRPC.Client.Services.*``.

To interact with a service, you must first instantiate it. The following example
connects to the server, instantiates the SpaceCenter service, and outputs the
name of the active vessel:

.. literalinclude:: /scripts/Interacting.cs

.. _csharp-client-streams:

Streaming Data from the Server
------------------------------

A stream repeatedly executes a function on the server, with a fixed set of
argument values. It provides a more efficient way of repeatedly getting the
result of a function, avoiding the network overhead of having to invoke it
directly.

For example, consider the following loop that continuously prints out the
position of the active vessel. This loop incurs significant communication
overheads, as the :meth:`KRPC.Client.Services.SpaceCenter.Vessel.Position`
method is called repeatedly.

.. literalinclude:: /scripts/Streaming.cs

The following code achieves the same thing, but is far more efficient. It calls
:meth:`Connection.AddStream` once at the start of the program to create a
stream, and then repeatedly gets the position from the stream.

.. literalinclude:: /scripts/Streaming2.cs

Streams are created for any method call by calling :meth:`Connection.AddStream`
and passing it a lambda expression calling the desired method. This lambda
expression must take zero arguments and be either a method call expression or a
parameter call expression. It returns an instance of the :type:`Stream` class
from which the latest value can be obtained by calling :meth:`Stream.Get`. A
stream can be stopped and removed from the server by calling
:meth:`Stream.Remove` on the stream object. All of a clients streams are
automatically stopped when it disconnects.

Client API Reference
--------------------

.. class:: Connection

   A connection to the kRPC server. All interaction with kRPC is performed via
   an instance of this class.

   .. method:: Connection (string name = "", IPAddress address = null, int rpcPort = 50000, int streamPort = 50001)

      Connect to a kRPC server on the specified IP address and port numbers. If
      streamPort is 0, does not connect to the stream server. Passes an optional
      name to the server to identify the client (up to 32 bytes of UTF-8 encoded
      text).

   .. method:: Stream<ReturnType> AddStream<ReturnType> (LambdaExpression expression)

      Create a new stream from the given lambda expression. Returns a stream
      object that can be used to obtain the latest value of the stream.

   .. method:: Dispose ()

      Close the connection and free any resources associated with it.

.. class:: Stream<ReturnType>

   Object representing a stream.

   .. method:: ReturnType Get ()

      Get the most recent value of the stream.

   .. method:: void Remove ()

      Remove the stream from the server.
