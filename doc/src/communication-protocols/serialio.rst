Protocol Buffers over Serial IO
================================

This communication protocol allows microcontrollers and other devices that communicate over a serial
port to interact with a kRPC server, for example an Arduino.

.. note::

   If a client library is available for your language, you do not need to implement this protocol.

.. note::

   Streams and events are not supported over serial IO. The server accepts the stream and event
   RPCs, but their updates have no flow control and can fill its write buffer, as described under
   :ref:`communication-protocol-serialio-buffering`.

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

Every message is a ``Message``. See :ref:`communication-protocol-message`.

Connecting to the Server
------------------------

There is a single serial port connection. To establish a connection, a client must do the
following:

1. Open a connection to the serial port.

2. Send a ``Message`` holding a ``connection_request``, with its ``protocol_version`` field set to
   2. See :ref:`communication-protocol-handshake`.

3. Receive a ``Message`` holding the server's ``connection_response``, and check that its
   ``status`` field is ``ConnectionResponse.OK`` and its ``protocol_version`` is 2.

A serial line has no connect or disconnect. A client ends its session by sending a ``disconnect``,
after which the server reads the next message as a ``connection_request``. A
``connection_request`` sent during a session also starts a new one, as a client does when it
resets.

Resetting Clients
-----------------

A client that resets, such as an Arduino after a power cycle, can find the line in the state its
earlier session left it in. The server ends a session whose message stalls for 3 seconds partway
through, and drops the partial message. A client that may be resuming a line follows these rules,
so that it can start a new session:

* It skips a ``response`` or ``stream_update`` while it waits for its ``connection_response``, as
  those can be left over from the earlier session.
* It treats a ``disconnect`` and the statuses ``MALFORMED_MESSAGE`` and ``TIMEOUT`` like no answer,
  as each can follow a request that landed in a stale partial message. It stays silent until its
  handshake timeout, which is more than 3 seconds, then sends the request again.
* It never retries after ``DENIED`` or ``WRONG_PROTOCOL_VERSION``.
* It keeps reading ``connection_response`` messages until the first ``response`` of the session.
  Retries queue on the line while the server is not reading, such as during a scene load, and the
  server answers each with a new session.

A server with "Auto-accept new clients" turned off answers a connection request only once the player
approves it. A client of such a server connects with no handshake timeout, so it recovers from a
stale partial message only by a manual reset.

Invoking Remote Procedures
--------------------------

See :doc:`messages`.

.. _communication-protocol-serialio-buffering:

Buffering and Data Rates
------------------------

A serial port carries data far more slowly than the game produces it. At the default 9600 baud it
carries roughly a thousand bytes per second. The server therefore buffers up to a megabyte of data
in each direction, and the two directions behave differently when their buffer fills:

* Data received from the port is buffered until the game reads it. When the buffer fills, the
  server stops reading from the port until the game catches up. The data is kept, and the port
  carries it more slowly.

* Data written by the game is buffered until the port has sent it. Apart from the start and end
  of each session, the game writes responses. This buffer fills when a client sends requests faster
  than it reads their responses. The server then drops the connection and stops. It can be started
  again from the in-game configuration window.

To keep the write buffer from filling, read each response before sending the next request. A higher
baud rate moves more data through the port.

Examples
--------

The following Python code connects to a server on serial port ``/dev/ttyUSB0`` using the name
"Jeb". It then invokes the ``KRPC.GetStatus`` RPC, receives and decodes the result, and prints out
the server version number from the response. Finally it ends the session with a ``disconnect``.

.. literalinclude:: /scripts/communication-protocols/serialio/RPC.py
   :language: python
