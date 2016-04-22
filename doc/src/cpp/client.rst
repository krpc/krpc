.. default-domain:: cpp
.. highlight:: cpp

C++ Client
==========

This client provides functionality to interact with a kRPC server from programs
written in C++. It can be :github-download-zip:`downloaded from GitHub <krpc-cpp>`.

Installing the Library
----------------------

Installing Dependencies
^^^^^^^^^^^^^^^^^^^^^^^

First you need to install kRPC's dependencies: `ASIO <http://think-async.com/>`_
which is used for network communication and `protobuf
<https://github.com/google/protobuf>`_ which is used to serialize messages.

ASIO is a headers-only library. The boost version is not required, installing
the non-Boost variant is sufficient. On Ubuntu, this can be done using apt:

.. code-block:: bash

   sudo apt-get install libasio-dev

Alternatively it can be downloaded `via the ASIO website
<http://think-async.com/Asio/Download>`_.

Protobuf version 3 is also required, and can be `downloaded from GitHub
<https://github.com/google/protobuf/releases>`_. Installation
instructions `can be found here
<https://github.com/google/protobuf/blob/master/src/README.md>`_.

.. note::

   The version of protobuf currently provided in Ubuntu's apt repositories is
   version 2. This will *not* work with kRPC.

Install using the configure script
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Once the dependencies have been installed, you can install the kRPC client
library and headers using the configure script provided with the
source. :github-download-zip:`Download the source archive <krpc-cpp>`, extract
it and then execute the following:

.. code-block:: bash

   ./configure
   make
   sudo make install
   sudo ldconfig

Install using CMake
^^^^^^^^^^^^^^^^^^^

Alternatively, you can install the client library and headers using
CMake. :github-download-zip:`Download the source archive <krpc-cpp>`, extract it
and execute the following:

.. code-block:: bash

   cmake .
   make
   sudo make install
   sudo ldconfig

Install manually
^^^^^^^^^^^^^^^^

The library is fairly simple to build manually if you can't use the configure
script or CMake. The headers are in the ``include`` folder and the source files
are in ``src``.

Using the Library
-----------------

kRPC programs need to be compiled with C++ 2011 support enabled, and linked
against ``libkrpc`` and ``libprotobuf``. The following example program connects
to the server, queries it for its version and prints it out:

.. literalinclude:: /scripts/Basic.cpp

To compile this program using GCC, save the source as ``main.cpp`` and run the
following:

.. code-block:: bash

   g++ main.cpp -std=c++11 -lkrpc -lprotobuf

.. note::

   If you get linker errors claiming that there are undefined references to
   ``google::protobuf::...`` you probably have an older version of protobuf
   installed on your system. In this case, replace ``-lprotobuf`` with
   ``-l:libprotobuf.so.10`` in the above command to force GCC to use the correct
   version of the library.

Connecting to the Server
^^^^^^^^^^^^^^^^^^^^^^^^

To connect to a server, use the :func:`krpc::connect` function. This returns a
client object through which you can interact with the server. When called
without any arguments, it will connect to the local machine on the default port
numbers. You can specify different connection settings, including a descriptive
name for the client, as follows:

.. literalinclude:: /scripts/Connecting.cpp

Interacting with the Server
^^^^^^^^^^^^^^^^^^^^^^^^^^^

kRPC groups remote procedures into services. The functionality for the services
are defined in the header files in ``krpc/services/...``. For example, all of
the functionality provided by the SpaceCenter service is contained in the header
file ``krpc/services/space_center.hpp``.

To interact with a service, you must include its header file and create an
instance of the service, passing a :class:`krpc::Client` object to its
constructor. The following example connects to the server, instantiates the
SpaceCenter service and outputs the name of the active vessel:

.. literalinclude:: /scripts/Interacting.cpp

.. _cpp-client-streams:

Streaming Data from the Server
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

A stream repeatedly executes a function on the server, with a fixed set of
argument values. It provides a more efficient way of repeatedly getting the
result of a function, avoiding the network overhead of having to invoke it
directly.

For example, consider the following loop that continuously prints out the
position of the active vessel. This loop incurs significant communication
overheads, as the ``vessel.position()`` function is called repeatedly.

.. literalinclude:: /scripts/Streaming.cpp

The following code achieves the same thing, but is far more efficient. It calls
``vessel.position_stream()`` once at the start of the program to create a
stream, and then repeatedly gets the position from the stream.

.. literalinclude:: /scripts/Streaming2.cpp

A stream can be created for any function call (except property setters) by
adding ``_stream`` to the end of the function's name. This returns a stream
object of type :class:`krpc::Stream<T>`, where ``T`` is the return type of the
original function. The most recent value of the stream can be obtained by
calling :func:`krpc::Stream<T>::operator()()`. A stream can be stopped and
removed from the server by calling :func:`krpc::Stream<T>::remove()` on the
stream object. All of a clients streams are automatically stopped when it
disconnects.

Client API Reference
--------------------

.. namespace:: krpc

.. function:: Client connect(const std::string& name = "", const std::string& address = "127.0.0.1", unsigned int rpc_port = 50000, unsigned int stream_port = 50001)

   This function creates a connection to a kRPC server. It returns a
   :class:`krpc::Client` object, through which the server can be communicated
   with.

   :parameters:

      * **name** (*std::string*) -- A descriptive name for the connection. This
        is passed to the server and appears, for example, in the client
        connection dialog on the in-game server window.
      * **address** (*std::string*) -- The address of the server to connect
        to. Can either be a hostname or an IP address in dotted decimal
        notation. Defaults to '127.0.0.1'.
      * **rpc_port** (*unsigned int*) -- The port number of the RPC
        Server. Defaults to 50000.
      * **stream_port** (*unsigned int*) -- The port number of the Stream
        Server. Defaults to 50001. Set it to 0 to disable connection to the
        stream server.

.. class:: Client

   This class provides the interface for communicating with the server. It is
   used by service class instances to invoke remote procedure calls. Instances
   of this class can be obtained by calling :func:`krpc::connect`.

.. namespace:: krpc::services

.. class:: KRPC

   This class provides access to the basic server functionality provided by
   the ``KRPC`` service. Most of this functionality is used internally by the
   client (for example to create and remove streams) and therefore does not
   need to be used directly from application code. The only exception that
   may be useful is :func:`KRPC::get_status`.

   .. function:: KRPC(krpc::Client* client)

      Construct an instance of this service from the given :class:`krpc::Client`
      object.

   .. function:: krpc::schema::Status get_status()

      Gets a status message from the server containing information including
      the server's version string and performance statistics.

      For example, the following prints out the version string for the
      server:

      .. literalinclude:: /scripts/Connecting.cpp

      Or to get the rate at which the server is sending and receiving data
      over the network:

      .. literalinclude:: /scripts/ServerStats.cpp

.. namespace:: krpc

.. class:: Stream<T>

   A stream object. Streams are created by calling a function with ``_stream``
   appended to its name.

   .. function:: T operator()()

      Get the most recently received value from the stream.

   .. function:: void remove()

      Remove the stream from the server.
