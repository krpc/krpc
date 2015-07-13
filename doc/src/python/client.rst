.. default-domain:: py

Python Client
=============

The ``krpc`` module provides functionality to interact with a kRPC server from
python.

Connecting to the Server
------------------------

To connect to a server, use the :func:`krpc.connect` function. This returns a
connection object through which you can interact with the server. For example to
connect to a server running on the local machine:

.. code-block:: python

   import krpc
   conn = krpc.connect(name='Example')
   print(conn.krpc.get_status().version)

This function also accepts arguments that specify what address and port numbers
to connect to. For example:

.. code-block:: python

   import krpc
   conn = krpc.connect(address='my.domain.name', rpc_port=1000, stream_port=1001, name = 'Remote example')
   print(conn.krpc.get_status().version)

Interacting with the Server
---------------------------

Interaction with the server is performed via the client object (of type
:class:`krpc.client.Client`) returned when connecting to the server using
:func:`krpc.connect`.

Upon connecting, the client interrogates the server to find out what
functionality it provides and dynamically adds all of the classes, methods,
properties to the client object.

For example, all of the functionality provided by the SpaceCenter service is
accessible via ``conn.space_center`` and the functionality provided by the
InfernalRobotics service is accessible via ``conn.infernal_robotics``. To
explore the functionality provided by a service, you can use the ``help()``
function from an interactive terminal. For example, running
``help(conn.space_center)`` will list all of the classes, enumerations, procedures and
properties provides by the SpaceCenter service. Or for a class, such as the
vessel class provided by the SpaceCenter service by calling
``help(conn.space_center.Vessel)``.

Calling methods, getting or setting properties, etc. are mapped to remote
procedure calls and passed to the server by the python client.

Streaming Data from the Server
------------------------------

A stream repeatedly executes a function on the server, with a fixed set of
argument values. It provides a more efficient way of repeatedly getting the
result of calling function on the server, without having to invoke it directly
-- which incurs communication overheads.

For example, consider the following loop that continuously prints out the
position of the active vessel. This loop incurs significant communication
overheads, as the ``vessel.position`` function is called repeatedly.

.. code-block:: python

   vessel = conn.space_center.active_vessel
   refframe = vessel.orbit.body.reference_frame
   while True:
       print vessel.position(refframe)

The following code achieves the same thing, but is far more efficient. It makes
a single call to :meth:`Client.add_stream` to create the stream, which avoids
the communication overhead in the previous example.

.. code-block:: python

   vessel = conn.space_center.active_vessel
   refframe = vessel.orbit.body.reference_frame
   position = conn.add_stream(vessel.position, refframe)
   while True:
       print position()

Streams are created by calling :meth:`Client.add_stream` or using the ``with``
statement applied to :meth:`Client.stream`. Both of these methods return an
instance of the :class:`krpc.stream.Stream` class.

Reference
---------

.. module:: krpc

.. function:: connect([address='127.0.0.1'], [rpc_port=50000], [stream_port=50001], [name=None])

   This function creates a connection to a kRPC server. It returns a
   :class:`krpc.client.Client` object, through which the server can be
   communicated with.

   :param string address: The address of the server to connect to. Can either be
                          a hostname or an IP address in dotted decimal
                          notation. Defaults to '127.0.0.1'.
   :param int rpc_port: The port number of the RPC Server. Defaults to 50000.
   :param int stream_port: The port number of the Stream Server. Defaults
                           to 50001.
   :param string name: A descriptive name for the connection. This is passed to
                       the server and appears, for example, in the client
                       connection dialog on the in-game server window.

.. module:: krpc.client

.. class:: Client

   This class provides the interface for communicating with the server. It is
   dynamically populated with all the functionality provided by the
   server. Instances of this class should be obtained by calling
   :func:`krpc.connect`.

   .. method:: add_stream(func, *args, **kwargs)

      Create a stream for the function *func* called with arguments *args* and
      *kwargs*. Returns a :class:`krpc.streams.Stream` object.

   .. method:: stream(func, *args, **kwargs)

      Allows use of the ``with`` statement to create a stream and automatically
      remove it from the server when it goes out of scope. The function to be
      streamed should be passed as *func*, and its arguments as *args* and
      *kwargs*.

      For example, to stream the result of method call
      ``vessel.position(refframe)``:

      .. code-block:: python

         vessel = conn.space_center.active_vessel
         refframe = vessel.orbit.body.reference_frame
         with conn.stream(vessel.position, refframe) as pos:
             print('Position =', pos())

      Or to stream the property ``conn.space_center.ut``:

      .. code-block:: python

         with conn.stream(getattr(conn.space_center, 'ut')) as ut:
             print('Universal Time =', ut())

   .. method:: close()

      Closes the connection to the server.

   .. attribute:: krpc

      The built-in KRPC class, providing basic interactions with the server.

      :rtype: :class:`krpc.client.KRPC`

.. class:: KRPC

      This class provides access to the basic server functionality provided by
      the ``KRPC`` service. An instance can be obtained by calling
      :attr:`krpc.client.Client.krpc`. Most of this functionality is used
      internally by the python client (for example to create and remove streams)
      and therefore does not need to be used directly from application code. The
      only exception that may be useful is:

      .. method:: get_status()

         Gets a status message from the server containing information including
         the server's version string and performance statistics.

         For example, the following prints out the version string for the
         server:

         .. code-block:: python

            print('Server version =', conn.krpc.get_status().version)

         Or to get the rate at which the server is sending and receiving data
         over the network:

         .. code-block:: python

            status = conn.krpc.get_status()
            print('Data in =', (status.bytes_read_rate/1024.0), 'KB/s')
            print('Data out =', (status.bytes_written_rate/1024.0), 'KB/s')

.. module:: krpc.stream

.. class:: Stream

   .. method:: __call__()

      Gets the most recently received value for the stream.

   .. method:: remove()

      Remove the stream from the server.
