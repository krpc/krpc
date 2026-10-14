Protocol Buffers over Serial IO
================================

This communication protocol allows microcontrollers and other devices that communicate over a serial
port to interact with a kRPC server, for example an Arduino.

.. note::

   If a client library is available for your language, you do not need to implement this protocol.

.. note::

   The serial IO protocol carries RPCs only. Streams and events are not available, and the stream
   procedures, such as ``KRPC.AddStream`` and ``KRPC.RemoveStream``, return an error.

Configuring the Server
----------------------

Unlike the TCP/IP and WebSockets protocols, the serial IO protocol is not enabled by default. To use
it, add a new server from the in-game server configuration window and select
*Protobuf over SerialIO* as the protocol. The configuration window allows the serial port name and
baud rate (which defaults to 9600) to be set.

Sending and Receiving Messages
------------------------------

Communication with the server is performed via Protocol Buffer messages, encoded according to the
protobuf binary format. When sending messages to and from the server, they are prefixed with their
size, in bytes, encoded as a Protocol Buffers varint.

To send a message to the server:

1. Encode the message using the Protocol Buffers format.
2. Get the length of the encoded message data (in bytes) and encode that as a Protocol Buffers
   varint.
3. Write the encoded length followed by the encoded message data to the serial port.

To receive a message from the server, do the reverse:

1. Read the size of the message as a Protocol Buffers encoded varint from the serial port.
2. Decode the message size.
3. Read message size bytes of message data from the serial port.
4. Decode the message data.

Unlike the TCP/IP and WebSockets protocols, all messages sent to the server after the initial
connection handshake are wrapped in a ``MultiplexedRequest`` message, and all messages received
from the server after the handshake are wrapped in a ``MultiplexedResponse`` message:

.. literalinclude:: /communication-protocols/krpc.proto
   :language: protobuf
   :start-at: message MultiplexedRequest {
   :end-at: StreamUpdate stream_update = 2;
   :append: }

The server leaves the ``stream_update`` field of a ``MultiplexedResponse`` unset.

Connecting to the Server
------------------------

There is a single serial port connection for RPC calls. To establish a connection, a client must
do the following:

1. Open a connection to the serial port.

2. Send a ``MultiplexedRequest`` message to the server with its ``connection_request`` field
   populated. The ``ConnectionRequest`` message is defined as:

   .. literalinclude:: /communication-protocols/krpc.proto
      :language: protobuf
      :start-at: message ConnectionRequest {
      :end-before: message ConnectionResponse {

   The ``type`` field should be set to ``ConnectionRequest.RPC`` and the ``client_name`` field can
   be set to the name of the client to display on the in-game UI. The ``client_identifier`` should
   be left blank.

3. Receive a ``ConnectionResponse`` message from the server. This message is defined as:

   .. literalinclude:: /communication-protocols/krpc.proto
      :language: protobuf
      :start-at: message ConnectionResponse {
      :end-before: // Messages for calling remote procedures

   If the ``status`` field is set to ``ConnectionResponse.OK`` then the connection was successful.
   If not, the ``message`` field contains a description of what went wrong.

   The server waits up to 3 seconds for the complete connection request message to arrive after
   its first bytes are received. If it does not arrive in time, the server responds with the
   ``TIMEOUT`` status.

Invoking Remote Procedures
--------------------------

See :doc:`messages`.

To send a procedure call, wrap the ``Request`` message in a ``MultiplexedRequest`` and set its
``request`` field. Receive the result as a ``MultiplexedResponse`` and read the ``Response`` from
its ``response`` field.

.. _communication-protocol-serialio-buffering:

Buffering and Data Rates
------------------------

A serial port carries data far more slowly than the game produces it. At the default 9600 baud it
carries roughly a thousand bytes per second. The server therefore buffers up to a megabyte of data
in each direction, and the two directions behave differently when their buffer fills:

* Data received from the port is buffered until the game reads it. When the buffer fills, the
  server stops reading from the port until the game catches up. The data is kept, and the port
  carries it more slowly.

* Data written by the game is buffered until the port has sent it. The game writes only responses,
  so this buffer fills when a client sends requests faster than it reads their responses. The
  server then drops the connection and stops. It can be started again from the in-game
  configuration window.

To keep the write buffer from filling, read each response before sending the next request. A higher
baud rate moves more data through the port.

Examples
--------

The following Python code connects to a server on serial port ``/dev/ttyUSB0`` using the name
"Jeb". It then invokes the ``KRPC.GetStatus`` RPC, receives and decodes the result, and prints out
the server version number from the response.

.. literalinclude:: /scripts/communication-protocols/serialio/RPC.py
   :language: python
