Protocol Buffers over TCP/IP
============================

This communication protocol allows languages that can communication over a TCP/IP connection to
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

Connecting to the RPC Server
----------------------------

A client invokes remote procedures by communicating with the *RPC server*. To establish a
connection, a client must do the following:

 1. Open a TCP socket to the server on its RPC port (which defaults to 50000).

 2. Send a ``ConnectionRequest`` message to the server. This message is defined as:

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

 3. Receive a `ConnectionResponse` message from the server. This message is defined as:

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

    If the ``status`` field is set to ``ConnectionResponse.OK`` then the connection was
    successful. If not, the ``message`` field contains a description of what went wrong.

    When the connection is successful, the ``client_identifier`` contains a unique 16-byte
    identifier for the client. This is required when connecting to the stream server (described
    below).

Connecting to the Stream Server
-------------------------------

Clients can receive :ref:`communication-protocol-streams` from the *stream server*. To establish a
connection, a client must first connect to the RPC server (as above) then do the following:

1. Open a TCP socket to the server on its stream port (which defaults to 50001).

2. Send a `ConnectionRequest` message, with its `type` field set to `ConnectionRequest.STREAM` and
   its `client_identifier` field set to the value received in the `client_identifier` field of the
   `ConnectionResponse` message received when connecting to the RPC server earlier.

3. Receive a `ConnectionResponse` message, similarly to the RPC server, and check that the value of
    the `status` field is `ConnectionResponse.OK`. If not, then the connection was not successful,
    and the `message` field contains a description of what went wrong.

Connecting to the stream server is optional. If the client doesn't require stream functionality,
there is no need to connect.

Invoking Remote Procedures
--------------------------

See :doc:`messages`.

Examples
--------

The following Python code connects to the RPC server at address 127.0.0.1 and port 50000 using the
name "Jeb". Next, it connects to the stream server on port 50001. It then invokes the
``KRPC.GetStatus`` RPC, receives and decodes the result and prints out the server version number
from the response.

The following python code connects to the RPC server at address 127.0.0.1 and port 50000, using the
name "Jeb". Next, it connects to the stream server on port 50001. Finally it invokes the
``KRPC.GetStatus`` procedure, and receives, decodes and prints the result.

To send and receive messages to the server, they need to be encoded and decoded from their binary
format:

 * The ``encode_varint`` and ``decode_varint`` functions convert between Python integers and
   Protocol Buffer varint encoded integers.
 * ``send_message`` encodes a message, sends the length of the message to the server as a Protocol
   Buffer varint encoded integer, and then sends the message data.
 * ``recv_message`` receives the size of the message, decodes it, receives the message data, and
   decodes it.

.. literalinclude:: /scripts/communication-protocol-rpc.py
   :language: python

The following example demonstrates how to set up and receive data from the server over a stream:

.. literalinclude:: /scripts/communication-protocol-stream.py
   :language: python
