Communication Protocols
=======================

kRPC provides various communication protocols:

* :doc:`communication-protocols/tcpip` for languages that can communicate over a
  TCP/IP socket.
* :doc:`communication-protocols/localsocket` for clients on the same machine as the game,
  which carries the same messages over a cheaper path than TCP/IP.
* :doc:`communication-protocols/websockets` for web browsers.
* :doc:`communication-protocols/serialio` for microcontrollers like Arduino.

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

A client that connects with a protocol version the server does not accept may be refused with the
status ``WRONG_PROTOCOL_VERSION`` in its connection response, or may fail to connect.

.. toctree::
   :hidden:

   communication-protocols/tcpip
   communication-protocols/localsocket
   communication-protocols/websockets
   communication-protocols/serialio
   communication-protocols/messages
