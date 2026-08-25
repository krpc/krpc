Protocol Buffers over Serial IO
================================

This communication protocol allows microcontrollers and other devices that communicate over a serial
port to interact with a kRPC server, for example an Arduino.

.. note:: If a client library is available for your language, you do not need to implement this
          protocol.

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

.. code-block:: protobuf

   message MultiplexedRequest {
     ConnectionRequest connection_request = 1;
     Request request = 2;
   }

   message MultiplexedResponse {
     Response response = 1;
     StreamUpdate stream_update = 2;
   }

Connecting to the Server
------------------------

There is a single serial port connection for both RPC calls and stream updates. To establish a
connection, a client must do the following:

1. Open a connection to the serial port.

2. Send a ``MultiplexedRequest`` message to the server with its ``connection_request`` field
   populated. The ``ConnectionRequest`` message is defined as:

   .. code-block:: protobuf

      message ConnectionRequest {
        Type type = 1;
        string client_name = 2;
        bytes client_identifier = 3;
        enum Type {
          RPC = 0;
          STREAM = 1;
        };
      }

   The ``type`` field should be set to ``ConnectionRequest.RPC`` and the ``client_name`` field can
   be set to the name of the client to display on the in-game UI. The ``client_identifier`` should
   be left blank.

3. Receive a ``ConnectionResponse`` message from the server. This message is defined as:

   .. code-block:: protobuf

      message ConnectionResponse {
        Status status = 1;
        enum Status {
          OK = 0;
          MALFORMED_MESSAGE = 1;
          TIMEOUT = 2;
          WRONG_TYPE = 3;
        }
        string message = 2;
        bytes client_identifier = 3;
      }

   If the ``status`` field is set to ``ConnectionResponse.OK`` then the connection was successful.
   If not, the ``message`` field contains a description of what went wrong.

   The server waits up to 3 seconds for the complete connection request message to arrive after
   its first bytes are received. If it does not arrive in time, the server responds with the
   ``TIMEOUT`` status.

Receiving Stream Updates
------------------------

Clients can receive :ref:`communication-protocol-streams` over the same serial port connection used
for RPC calls. Stream updates arrive as ``MultiplexedResponse`` messages with the ``stream_update``
field populated. After sending an RPC request, the client should inspect each received
``MultiplexedResponse`` to determine whether it contains a ``response`` (the result of an RPC call)
or a ``stream_update`` (a stream result).

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

* Data written by the game is buffered until the port has sent it. When this buffer fills, which
  means the game has been producing data faster than the port can send for a sustained period,
  the server drops the connection and stops. It can be started again from the in-game
  configuration window. The easiest way to hit this limit is to request streams whose updates add
  up to more than the port's data rate, as an update to a stream is sent every game update.

To stay within the port's capacity, keep the number and size of streams small relative to the baud
rate, or configure a higher baud rate.

Examples
--------

The following Python code connects to a server on serial port ``/dev/ttyUSB0`` using the name
"Jeb". It then invokes the ``KRPC.GetStatus`` RPC, receives and decodes the result, and prints out
the server version number from the response.

.. literalinclude:: /scripts/communication-protocol-serialio.py
   :language: python
