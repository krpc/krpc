Protocol Buffers over WebSockets
================================

This communication protocol allows web browsers to communicate with kRPC over a websockets
connection, for example from Javascript in a web browser.

.. note:: If a client library is available for your language, you do not need to implement this
          protocol.

Sending and Receiving Messages
------------------------------

Communication with the server is performed via ``Message`` Protocol Buffer messages, encoded
according to the protobuf binary format. Each one is carried in one binary websockets message,
which a client may split across fragments.

To send a message to the server:

1. Encode the message using the Protocol Buffers format.
2. Send a binary websockets message to the server, with payload containing the encoded message
   data.

To receive a message from the server, do the reverse:

1. Receive a binary websockets message from the server.
2. Decode the message's payload using the Protocol Buffers format.

Connecting to the Server
------------------------

To establish a connection, a client must do the following:

1. Open a websockets connection to the server on its port (which defaults to 50000), for example
   ``ws://localhost:50000/``. The path must be ``/``, and the server answers any other path with
   HTTP 404. The upgrade carries no kRPC parameters: the server ignores a query string and a
   subprotocol, and names no subprotocol in its response.

2. Send a binary websockets message holding a ``Message`` with a ``connection_request``, with its
   ``protocol_version`` field set to 2. See :ref:`communication-protocol-handshake`.

3. Receive a binary websockets message holding the server's ``connection_response``, and check
   that its ``status`` field is ``ConnectionResponse.OK`` and its ``protocol_version`` is 2.

The server times the handshake from the moment it accepts the TCP connection, so the upgrade and the
``connection_request`` have 3 seconds between them to arrive. The server closes a connection
whose upgrade has not completed by then, with no response.

The connection then carries the client's requests and their responses, and the updates for its
:ref:`communication-protocol-streams`. To end it, send a ``disconnect`` and then a close frame. See
:ref:`communication-protocol-disconnect`.

Invoking Remote Procedures
--------------------------

See :doc:`messages`.

Examples
--------

The following code connects to the server at address 127.0.0.1 and port 50000 using the name "Jeb".
It then invokes the ``KRPC.GetStatus`` RPC, receives and decodes the result and prints out the
server version number from the response. Finally it ends the connection.

.. literalinclude:: /scripts/communication-protocols/websockets/RPC.js
   :language: javascript

The following example adds a stream, then receives its updates on the same connection:

.. literalinclude:: /scripts/communication-protocols/websockets/Stream.js
   :language: javascript
