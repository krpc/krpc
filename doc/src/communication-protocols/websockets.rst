Protocol Buffers over WebSockets
================================

This communication protocol allows web browsers to communicate with a kRPC over a websockets
connection, for example from Javascript in a web browser.

.. note:: If a client library is available for your language, you do not need to implement this
          protocol.

Connecting to the RPC Server
----------------------------

A client invokes remote procedures by communicating with the *RPC server*. To establish a
connection, open a websockets connection to the server on its RPC port (which defaults to
50000). The connection URI can also contain a ``name`` parameter with the name of the client to
display in the on the in-game UI. For example: ``ws://localhost:50000/?name=Jeb``

Connecting to the Stream Server
-------------------------------

Clients can receive :ref:`communication-protocol-streams` from the *stream server*. To establish a
connection, a client must first connect to the RPC server (as above) then do the following:

1. Get the identifier of the client by calling the ``KRPC.GetClientID`` remote procedure.

2. Open a websockets connection to the server on its stream port (which defaults to 50001), with an
   ``id`` query parameter set to the client identifier encoded in base64. For example:
   ``ws://localhost:50001/?id=Eeh8Vbj2DkWTcJZTkYlEhQ==``

Connecting to the stream server is optional. If the client doesn't require stream functionality,
there is no need to connect.

Sending and Receiving Messages
------------------------------

Communication with the server is performed via Protocol Buffer messages, encoded according to the
protobuf binary format.

To send a message to the server:

 1. Encode the message using the Protocol Buffers format.
 2. Send a binary websockets message to the server, with payload containing the encoded message
    data.

To receive a message from the server, do the reverse:

 1. Receive a binary websockets message from the server.
 2. Decode the messages payload using the Protocol Buffers format.

Invoking Remote Procedures
--------------------------

See :doc:`messages`.

Examples
--------

The following code connects to the RPC server at address 127.0.0.1 and port 50000 using
the name "Jeb". Next, it connects to the stream server on port 50001. It then invokes the
``KRPC.GetStatus`` RPC, receives and decodes the result and prints out the server version number
from the response.

.. literalinclude:: /scripts/communication-protocol-rpc.js
   :language: javascript

The following example demonstrates how to set up and receive data from the server over a stream:

.. literalinclude:: /scripts/communication-protocol-stream.js
   :language: javascript
