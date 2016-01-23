.. default-domain:: cpp

C++ Client
==========

The ``krpc`` library provides functionality to interact with a kRPC server from
C++.

Connecting to the Server
------------------------

To connect to a server, use the :func:`krpc::connect` function. This returns a
connection object through which you can interact with the server. For example to
connect to a server running on the local machine:

.. code-block:: cpp

   #include <krpc/krpc.hpp>
   #include <krpc/services/krpc.hpp>
   #include <iostream>

   using namespace krpc;

   int main() {
     krpc::Client conn = krpc::connect("Example");
     krpc::services::KRPC krpc(&conn);
     std::cout << krpc.get_status().version() << std::endl;
   }

This function also accepts arguments that specify what address and port numbers
to connect to. For example:

.. code-block:: cpp

   #include <krpc/krpc.hpp>
   #include <krpc/services/krpc.hpp>
   #include <iostream>

   using namespace krpc;

   int main() {
     krpc::Client conn = krpc::connect("Remote example", "my.domain.name", 1000, 1001);
     krpc::services::KRPC krpc(&conn);
     std::cout << krpc.get_status().version() << std::endl;
   }

Interacting with the Server
---------------------------

Interaction with the server is performed via a client object (of type
:class:`krpc::Client`) returned by calling :func:`krpc::connect`.

Functionality for services are defined in the header files in
``krpc/services/...``. For example, all of the functionality provided by the
SpaceCenter service is contained in the header file
``krpc/services/space_center.hpp`` and the functionality provided by the
InfernalRobotics service is contained in
``krpc/services/infernal_robotics.hpp``.

Before a service can be used it must first be instantiated, and passed a copy of
the :class:`krpc::Client` object. Calling methods on the service are mapped to
remote procedure calls and passed to the server by the client.

The following example connects to the server, instantiates the SpaceCenter
service, and outputs the name of the active vessel:

.. code-block:: cpp

   #include <krpc/krpc.hpp>
   #include <krpc/services/space_center.hpp>
   #include <iostream>

   using namespace krpc;

   int main() {
     krpc::Client conn = krpc::connect("Vessel Name");
     krpc::services::SpaceCenter sc(&conn);
     krpc::services::SpaceCenter::Vessel vessel = sc.active_vessel();
     std::cout << vessel.name() << std::endl;
   }

Streaming Data from the Server
------------------------------

Streams are not yet supported by the C++ client.

Reference
---------

.. namespace:: krpc

.. function:: krpc::Client connect(const std::string& name = "", const std::string& address = "127.0.0.1", unsigned int rpc_port = 50000, unsigned int stream_port = 50001)

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

         .. code-block:: cpp

            #include <krpc/krpc.hpp>
            #include <krpc/services/krpc.hpp>
            #include <iostream>

            using namespace krpc;

            int main() {
              krpc::Client conn = krpc::connect();
              krpc::services::KRPC krpc(&conn);
              std::cout << "Server version = " << krpc.get_status().version() << std::endl;
            }

         Or to get the rate at which the server is sending and receiving data
         over the network:

         .. code-block:: cpp

            #include <krpc/krpc.hpp>
            #include <krpc/services/krpc.hpp>
            #include <iostream>

            using namespace krpc;

            int main() {
              krpc::Client conn = krpc::connect();
              krpc::services::KRPC krpc(&conn);
              krpc::schema::Status status = krpc.get_status();
              std::cout << "Data in = " << (status.bytes_read_rate()/1024.0) << " KB/s" << std::endl;
              std::cout << "Data out = " << (status.bytes_written_rate()/1024.0) << " KB/s" << std::endl;
            }
