Communication Protocols
=======================

kRPC provides four communication protocols:

* :doc:`communication-protocols/tcpip` for languages that can communicate over a
  TCP/IP socket.
* :doc:`communication-protocols/localsocket` for clients on the same machine as the game,
  which carries the same messages over a cheaper path than TCP/IP.
* :doc:`communication-protocols/websockets` for web browsers.
* :doc:`communication-protocols/serialio` for Arduino and similar.

In each protocol, clients invoke remote procedure calls by :doc:`sending and receiving protobuf
messages <communication-protocols/messages>`.

.. _communication-protocol-version:

Protocol Version
----------------

Each connection handshake carries the version of the protocol, an integer. The client sends the
version it uses, and the server replies with the version it accepted:

.. list-table::
   :header-rows: 1

   * - Protocol version
     - kRPC release
     - Server accepts
   * - 0
     - Before 0.7
     - Any version, which it ignores
   * - 1
     - 0.7
     - 0 and 1
   * - 2
     - 0.8
     - 2

Version 2 puts every message in a :ref:`Message <communication-protocol-message>`, on every
transport. A client from an earlier release fails to connect. The server refuses a
``connection_request`` with a version other than 2 with the status ``WRONG_PROTOCOL_VERSION``.

.. toctree::
   :hidden:

   communication-protocols/tcpip
   communication-protocols/localsocket
   communication-protocols/websockets
   communication-protocols/serialio
   communication-protocols/messages
