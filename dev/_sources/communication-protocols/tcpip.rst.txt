Protocol Buffers over TCP/IP
============================

This communication protocol allows languages that can communicate over a TCP/IP connection to
interact with a kRPC server.

.. note:: If a client library is available for your language, you do not need to implement this
          protocol.

Sending and Receiving Messages
------------------------------

Communication with the server is performed via Protocol Buffer messages, encoded according to the
protobuf binary format. When sending messages to and from the server, they are prefixed with their
size, in bytes, encoded as a Protocol Buffers varint.

To send a message to the server:

1. Encode the message using the Protocol Buffers format.
2. Get the length of the encoded message data (in bytes) and encode that as a Protocol Buffers
   varint.
3. Send the encoded length followed by the encoded message data.

To receive a message from the server, do the reverse:

1. Receive the size of the message as a Protocol Buffers encoded varint.
2. Decode the message size.
3. Receive message size bytes of message data.
4. Decode the message data.

Every message is a ``Message``. See :ref:`communication-protocol-message`.

Connecting to the Server
------------------------

To establish a connection, a client must do the following:

1. Open a TCP socket to the server on its port (which defaults to 50000).

2. Send a ``Message`` holding a ``connection_request``, with its ``protocol_version`` field set to
   2. See :ref:`communication-protocol-handshake`.

3. Receive a ``Message`` holding the server's ``connection_response``, and check that its
   ``status`` field is ``ConnectionResponse.OK`` and its ``protocol_version`` is 2. If not, the
   ``message`` field contains a description of what went wrong.

The connection then carries the client's requests and their responses, and the updates for its
:ref:`communication-protocol-streams`. To end it, see :ref:`communication-protocol-disconnect`.

Invoking Remote Procedures
--------------------------

See :doc:`messages`.

Examples
--------

The following Python code connects to the server at address 127.0.0.1 and port 50000 using the
name "Jeb". It then invokes the ``KRPC.GetStatus`` RPC, receives and decodes the result and prints
out the server version number from the response. Finally it ends the connection.

To send and receive messages to the server, they need to be encoded and decoded from their binary
format:

* The ``encode_varint`` and ``decode_varint`` functions convert between Python integers and
  Protocol Buffer varint encoded integers.
* ``send_message`` encodes a message, sends the length of the message to the server as a Protocol
  Buffer varint encoded integer, and then sends the message data.
* ``recv_message`` receives the size of a message, decodes it, receives the message data, and
  decodes it as a ``Message``.

.. literalinclude:: /scripts/communication-protocols/tcpip/RPC.py
   :language: python

The following example adds a stream, then receives its updates on the same connection:

.. literalinclude:: /scripts/communication-protocols/tcpip/Stream.py
   :language: python
