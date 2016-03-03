.. default-domain:: py
.. highlight:: py

Python Client
=============

This client provides functionality to interact with a kRPC server from programs
written in Python. It can be `installed using PyPI
<https://pypi.python.org/pypi/krpc>`_ or :github-download-zip:`downloaded from
GitHub <krpc-python>`.

Installing the Library
----------------------

The python client and all of its dependencies can be installed using pip with a
single command. It supports Python 2.7+ and 3.x

On linux:

.. code-block:: bash

   pip install krpc

On Windows:

.. code-block:: none

   C:\Python27\Scripts\pip.exe install krpc

Using the Library
-----------------

Once it's installed, simply ``import krpc`` and you are good to go! You can
check what version you have installed by running the following script:

.. code-block:: python

   import krpc
   print(krpc.__version__)

Connecting to the Server
------------------------

To connect to a server, use the :func:`krpc.connect` function. This returns a
connection object through which you can interact with the server. For example to
connect to a server running on the local machine:

.. literalinclude:: /scripts/Basic.py

This function also accepts arguments that specify what address and port numbers
to connect to. For example:

.. literalinclude:: /scripts/Connecting.py

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
``help(conn.space_center)`` will list all of the classes, enumerations,
procedures and properties provides by the SpaceCenter service. Or for a class,
such as the vessel class provided by the SpaceCenter service by calling
``help(conn.space_center.Vessel)``.

Calling methods, getting or setting properties, etc. are mapped to remote
procedure calls and passed to the server by the python client.

Streaming Data from the Server
------------------------------

A stream repeatedly executes a function on the server, with a fixed set of
argument values. It provides a more efficient way of repeatedly getting the
result of a function, avoiding the network overhead of having to invoke it
directly.

For example, consider the following loop that continuously prints out the
position of the active vessel. This loop incurs significant communication
overheads, as the ``vessel.position`` function is called repeatedly.

.. literalinclude:: /scripts/Streaming.py

The following code achieves the same thing, but is far more efficient. It calls
:meth:`Client.add_stream` once at the start of the program to create a stream,
and then repeatedly gets the position from the stream.

.. literalinclude:: /scripts/Streaming2.py

A stream can be created by calling :meth:`Client.add_stream` or using the
``with`` statement applied to :meth:`Client.stream`. Both of these approaches
return an instance of the :class:`krpc.stream.Stream` class.

Both methods and attributes can be streamed. The example given above
demonstrates how to stream methods. The following example shows how to stream an
attribute (in this case ``vessel.control.abort``):

.. literalinclude:: /scripts/Streaming3.py

Client API Reference
--------------------

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

      .. literalinclude:: /scripts/Streaming4.py

      Or to stream the property ``conn.space_center.ut``:

      .. literalinclude:: /scripts/Streaming5.py

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

         .. literalinclude:: /scripts/ServerVersion.py

         Or to get the rate at which the server is sending and receiving data
         over the network:

         .. literalinclude:: /scripts/ServerStats.py

.. module:: krpc.stream

.. class:: Stream

   .. method:: __call__()

      Gets the most recently received value for the stream.

   .. method:: remove()

      Remove the stream from the server.
