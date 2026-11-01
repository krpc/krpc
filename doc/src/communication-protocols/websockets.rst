WebSockets
==========

This protocol carries the :doc:`messages <messages>` over a websockets connection, for a client in
a web browser.

.. note:: If a client library is available for your language, you do not need to implement this
          protocol.

Framing
-------

Each message is a ``Message``, encoded according to the Protocol Buffers binary format and carried
in one binary websockets message. The websockets message may be split across fragments.

Connecting
----------

To use this protocol, add a server in the server window and select *Protobuf over WebSockets* as
its protocol. Its address and port default to ``127.0.0.1`` and ``50000``.

Open a websockets connection to the server with the path ``/``, for example
``ws://127.0.0.1:50000/``. The upgrade request names no subprotocol. Then send the
:ref:`connection handshake <communication-protocol-handshake>`. The upgrade and the
``connection_request`` together have 3 seconds to arrive, timed from the TCP connection.

The connection carries the requests, the responses and the stream updates. To end it, send a
:ref:`disconnect <communication-protocol-disconnect>` and then a close frame.

Examples
--------

.. note::

   These examples show the protocol, and omit much of the behavior a client library needs, such as
   handling errors and failed connections. A client library should
   implement the :doc:`client specification </client-spec>` in its entirety.

The examples run in Node.js, using the ``ws`` and ``protobufjs`` packages. A web browser provides
the same ``WebSocket`` interface.

The following script connects to the server with the name "Example", calls ``KRPC.GetStatus`` and
prints the server version. It then ends the connection:

.. literalinclude:: /scripts/communication-protocols/websockets/RPC.js
   :language: javascript

The following script adds a stream, then receives its updates on the same connection:

.. literalinclude:: /scripts/communication-protocols/websockets/Stream.js
   :language: javascript
