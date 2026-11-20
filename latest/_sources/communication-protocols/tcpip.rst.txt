TCP/IP
======

This protocol carries the :doc:`messages <messages>` over a TCP/IP connection.

.. note:: If a client library is available for your language, you do not need to implement this
          protocol.

Framing
-------

Each message is a ``Message``, encoded according to the Protocol Buffers binary format and
prefixed with its size in bytes, encoded as a varint. The following Python functions send and
receive a message:

.. literalinclude:: /scripts/communication-protocols/tcpip/RPC.py
   :language: python
   :start-at: def encode_varint
   :end-at: return msg

Connecting
----------

Open a TCP socket to the server's address and port, which default to ``127.0.0.1`` and ``50000``.
Then send the :ref:`connection handshake <communication-protocol-handshake>`. The connection
carries the requests, the responses and the stream updates until either side sends a
:ref:`disconnect <communication-protocol-disconnect>`.

Examples
--------

.. note::

   These examples show the protocol, and omit much of the behavior a client library needs, such as
   handling errors and failed connections. A client library should
   implement the :doc:`client specification </client-spec>` in its entirety.

The following script connects to the server with the name "Example", calls ``KRPC.GetStatus`` and
prints the server version. It then ends the connection:

.. literalinclude:: /scripts/communication-protocols/tcpip/RPC.py
   :language: python

The following script adds a stream, then receives its updates on the same connection:

.. literalinclude:: /scripts/communication-protocols/tcpip/Stream.py
   :language: python
